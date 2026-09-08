import tensorflow as tf
import numpy as np
import pandas as pd #csv 읽기 라이브러리. csv 파일은 각 열을 쉼표로 구분하는 텍스트 파일로, 엑셀과 거의 유사
import random
random.seed(42)
np.random.seed(42)
tf.random.set_seed(42)
df = pd.read_csv("stock_history.csv") #csv 파일을 표로 읽음. df는 데이터프래임

# 1) 뉴스 종류(없음/소형/대형)에 따라 변동률 차이가 있는가?
# 예전엔 isAffectedByNews(켜짐/꺼짐) 하나만 봤는데, 이제 newsCategory로 종류가 나뉜다
print(df.groupby("newsCategory")["priceChangePct"].describe())

# 2) 시대 효과에 따라 변동률 차이가 있는가? (eraEffect는 -1 타격/0 무관/1 수혜, 예전과 동일)
print(df.groupby("eraEffect")["priceChangePct"].describe())

# 3) "어제 변동률"과 "오늘 변동률"이 상관관계가 있는가? (자기상관)
df_sorted = df.sort_values(["stockName", "day"])
df_sorted["prevChangePct"] = df_sorted.groupby("stockName")["priceChangePct"].shift(1)
print(df_sorted[["priceChangePct", "prevChangePct"]].corr())

# 슬라이딩 윈도우로 X, y 만들기
window_size = 10 #과거 10일치를 보고 내일을 예측

# 분기 길이(턴). GameBalance.QUARTER_LENGTH와 반드시 같아야 한다 — quarterDayIndex를 0~1로 정규화하는 데 쓴다
# 이 값이 어긋나면 학습 때와 NPCInvestor.cs 추론 때의 입력 스케일이 달라져서 예측이 망가진다
QUARTER_LENGTH = 20

# NPCInvestor.cs가 텐서에 값을 넣는 순서와 반드시 똑같아야 함 (정규화 가격은 맨 앞에 따로 붙는다)
FEATURE_COLUMNS = [
    "priceChangePct",
    "newsCategory",
    "isForeshadowPending",
    "daysSinceLastSmallNews",
    "daysSinceLastBigNews",
    "eraRatePct",
    "volatilityType",
    "quarterDayIndex",
    "isQuarterEndToday",
    "hasActiveBusiness",
    "personality",
    "fairPriceRatio",   # 목표주가/현재주가 — 분기말 변동의 방향을 정하는 값. 맨 끝에 추가했다
]
N_FEATURES = len(FEATURE_COLUMNS) + 1 #+1 = 정규화된 가격. 예전엔 3개(가격, 변동률, 뉴스여부)였던 게 13개로 늘어남

# X, y를 한 리스트에 다 모으는 대신, 처음부터 train/test로 나눠 담기
X_train, y_train = [], []
X_test, y_test = [], []
news_category_test = [] #테스트셋 샘플별 뉴스 상태를 따로 기록 — 나중에 카테고리별 정확도 확인용

# CSV는 게임을 돌릴 때마다 계속 덧붙여지므로 여러 판이 한 파일에 섞여 있다.
# 종목만으로 묶고 날짜로 정렬하면 같은 day를 가진 다른 판의 행이 한 윈도우에 섞여 들어간다
# (실제로 그렇게 학습했다가 방향 정확도가 0.53까지 떨어졌던 적이 있다).
# runId까지 묶어야 "한 판의 연속된 하루하루"만 윈도우가 된다
if "runId" not in df.columns:
    df["runId"] = "legacy"  # runId 컬럼이 없던 시절의 옛 CSV도 읽히게 (전부 한 판으로 취급)

# 새로 추가된 feature가 없는 옛 CSV로 학습하면, 학습 때는 그 자리가 비어 있는데 게임에서는 실제 값이
# 들어와서 예측이 조용히 망가진다. 그런 사고는 원인을 찾기가 매우 어려우므로 여기서 바로 멈춘다
missing = [c for c in FEATURE_COLUMNS if c not in df.columns]
if missing:
    raise SystemExit(
        f"\n[중단] stock_history.csv에 다음 컬럼이 없습니다: {missing}\n"
        f"        게임을 한 판 새로 돌려서 CSV를 다시 만든 뒤 Model/ 폴더로 복사하세요.\n"
        f"        (기존 파일에 덧붙이지 말고 지우고 새로 만들어야 합니다 — 컬럼 수가 달라졌습니다)\n")

print(f"판(runId) 수: {df['runId'].nunique()} / 종목 수: {df['stockName'].nunique()}")

for (stock_name, run_id), stock_df in df.groupby(["stockName", "runId"]):
    stock_df = stock_df.sort_values("day")
    prices = stock_df["currentPrice"].values
    change_pct = stock_df["priceChangePct"].values
    news_category = stock_df["newsCategory"].values

    # 나머지 feature들은 값의 범위가 제각각이라(예: daysSinceLastBigNews는 0~50대, quarterDayIndex는 0~19)
    # NPCInvestor.cs와 똑같은 비율로 나눠서 스케일을 맞춰준다
    features = stock_df[FEATURE_COLUMNS].copy()
    features["daysSinceLastSmallNews"] /= 10.0
    features["daysSinceLastBigNews"] /= 50.0
    features["quarterDayIndex"] /= QUARTER_LENGTH
    features = features.values

    n = len(prices)
    train_end = int(n * 0.8)

    price_min = prices[:train_end].min()
    price_max = prices[:train_end].max()
    if price_max == price_min:
        continue

    prices_normalized = (prices - price_min) / (price_max - price_min)
    prices_normalized = np.clip(prices_normalized, 0, 1.5)

    # 하루치 입력 한 줄 = [정규화 가격, feature들...] — 이 종목의 전체 날짜를 한 판에 만들어둔다
    # float32로 만드는 이유: TensorFlow가 어차피 float32로 쓰고, float64 대비 메모리가 절반이다
    day_rows = np.column_stack([prices_normalized, features]).astype(np.float32)

    # 윈도우를 파이썬 리스트로 쌓으면 값 하나하나가 파이썬 float 객체가 되어 메모리를 엄청나게 먹는다
    # (10만 행짜리 CSV에서 실제로 메모리 부족으로 죽었다). 배열을 미리 잡아두고 슬라이스로 채우면
    # 같은 데이터가 수십 MB로 줄어든다
    n_windows = n - window_size
    if n_windows <= 0:
        continue

    group_x = np.empty((n_windows, window_size, N_FEATURES), dtype=np.float32)
    for k in range(n_windows):
        i = k + window_size            # 예측 대상이 되는 날
        group_x[k] = day_rows[i - window_size:i]   # 그 전 10일치가 입력

    targets = change_pct[window_size:].astype(np.float32)   # 각 윈도우가 맞혀야 할 정답
    categories = news_category[window_size:]
    is_train = np.arange(window_size, n) < train_end        # ★ 여기서 train/test로 분배

    X_train.append(group_x[is_train])
    y_train.append(targets[is_train])
    X_test.append(group_x[~is_train])
    y_test.append(targets[~is_train])
    news_category_test.append(categories[~is_train])

# 종목·판별로 만든 조각들을 하나로 이어 붙인다
x_train = np.concatenate(X_train)
y_train = np.concatenate(y_train)
x_test = np.concatenate(X_test)
y_test = np.concatenate(y_test)
news_category_test = np.concatenate(news_category_test)

# 조각 리스트는 여기서 역할이 끝난다 — 붙여놓은 배열과 똑같은 데이터를 두 벌 들고 있을 이유가 없다
del X_train, X_test

print(f"훈련: {len(x_train)}개 / 테스트: {len(x_test)}개")

# 출력을 [기댓값, log(분산)] 2개로 늘려서, 평균뿐 아니라 "확신도"도 같이 학습시킨다
# log(분산)으로 예측하는 이유: 분산은 항상 양수여야 하는데, exp(log_var)로 복원하면 부호 제약을 신경 안 써도 됨
def gaussian_nll_loss(y_true, y_pred):
    mean = y_pred[:, 0]
    log_var = tf.clip_by_value(y_pred[:, 1], -10.0, 10.0)  # exp() 폭주 방지용 클리핑
    precision = tf.exp(-log_var)
    return tf.reduce_mean(0.5 * (log_var + precision * tf.square(y_true - mean)))

def mean_mae(y_true, y_pred):  # 기댓값 head만 떼서 기존 MAE 지표처럼 확인용
    return tf.reduce_mean(tf.abs(y_true - y_pred[:, 0]))

model = tf.keras.models.Sequential()
model.add(tf.keras.layers.LSTM(units=32, return_sequences=True))
model.add(tf.keras.layers.Dropout(rate=0.25))   # LSTM "뒤"로 이동
model.add(tf.keras.layers.LSTM(units=32))
model.add(tf.keras.layers.Dropout(rate=0.25))
model.add(tf.keras.layers.Dense(units=2, activation="linear"))  # [0]=기댓값, [1]=log(분산)
model.compile(
    loss = gaussian_nll_loss, #평균과 분산을 동시에 맞추는 가우시안 negative log-likelihood
    optimizer=tf.keras.optimizers.Adam(), #학습 방향을 조정하는 알고리즘으로, Adam이 가장 많이 쓰이는 옵티마이저
    metrics = [mean_mae] #기댓값 head 기준 평균 절대 오차
)
model.build(input_shape=(None, window_size, N_FEATURES)) #모델에 입력 형태 알려주기. 특성 개수가 3 → 12로 늘어남
model.summary()#모델 구조를 표로 출력

callbacks = [
    tf.keras.callbacks.EarlyStopping(
        monitor="val_loss", patience=5, restore_best_weights=True
    ),  # val_loss가 5 epoch 동안 안 좋아지면 멈추고, 가장 좋았던 가중치로 복원
    tf.keras.callbacks.ModelCheckpoint(
        "best_model.keras", monitor="val_loss", save_best_only=True
    ),  # 가장 성능 좋은 시점의 모델을 별도 파일로 저장
]

history = model.fit(
    x_train, y_train,
    batch_size=256,
    epochs=100,              # ★ 20 → 100으로 늘리되 EarlyStopping이 알아서 멈춤
    validation_split=0.2,
    callbacks=callbacks,
    verbose=1
)
raw_predicted = model.predict(x_test) #테스트 데이터로 예측 → [:,0]=기댓값, [:,1]=log(분산)
predicted = raw_predicted[:, 0]              #기댓값 head
predicted_log_var = raw_predicted[:, 1]      #log(분산) head
predicted_std = np.exp(0.5 * predicted_log_var) #표준편차로 환산 (분산 = exp(log_var))

# 방법 B — 평균 오차
mae = np.mean(np.abs(predicted - y_test)) #예측값과 실제값 차이의 절댓값 평균을 냄. abs가 절댓값.
print(f"평균 예측 오차: {mae:.4f}%")
print(f"예측 표준편차 평균: {predicted_std.mean():.4f} (최소 {predicted_std.min():.4f} / 최대 {predicted_std.max():.4f})")

# 방법 A — 방향 정확도
threshold = predicted.mean()  # 예측값들의 평균을 임계값으로 사용하여, 평균보다 높으면 상승, 낮으면 하락으로 판단
predicted_direction = (predicted > threshold).astype("int32") #높으면 True 1, 낮으면 False 0
actual_direction = (y_test > 0).astype("int32") #실제 변동률이 양수면 1 상승, 음수면 0

direction_accuracy = np.mean(predicted_direction == actual_direction) #예측 방향이 실제 방향과 일치한 비율
print(f"방향 정확도: {direction_accuracy:.4f}")
print(f"임계값: {threshold:.4f}")#여기보다 높으면 상승, 낮으면 하락"으로 나누는 기준선

# 추가 — 상승/하락 각각 얼마나 맞췄는지
up_mask = actual_direction == 1 #실제 상승한 날들
down_mask = actual_direction == 0 #실제 하락한 날들
print(f"상승 맞춘 비율: {np.mean(predicted_direction[up_mask] == actual_direction[up_mask]):.4f}")
print(f"하락 맞춘 비율: {np.mean(predicted_direction[down_mask] == actual_direction[down_mask]):.4f}")
print(f"예측값 평균: {predicted.mean():.4f}")
print(f"예측값 최소: {predicted.min():.4f}")
print(f"예측값 최대: {predicted.max():.4f}")
print(f"실제 상승 비율: {np.mean(y_test > 0):.4f}")

# 뉴스 상태별 방향 정확도 — 게임 자체가 상승장 편향이라 "뉴스 없음"이 테스트셋 대부분을 차지한다.
# 그래서 전체 평균만 보면 뉴스 있는 날 모델이 실제로 반응하는지가 묻혀버려서, 카테고리별로 쪼개서 확인한다
print("\n뉴스 상태별 방향 정확도")
for category, label in [(0, "뉴스 없음"), (1, "소형 뉴스"), (2, "대형 뉴스")]:
    mask = news_category_test == category
    if mask.sum() == 0:
        continue
    acc = np.mean(predicted_direction[mask] == actual_direction[mask])
    print(f"  {label} ({mask.sum()}개): {acc:.4f}")

import tf2onnx
import onnx
import os
import shutil

# ① keras로 저장
model.save("stock_model.keras")
print("keras 저장 완료")

# ② 한글 없는 경로로 저장
save_path = "C:/temp/stock_saved_model"

if os.path.exists(save_path):
    shutil.rmtree(save_path)

os.makedirs("C:/temp", exist_ok=True)

# ③ saved_model 저장
tf.saved_model.save(model, save_path)
print("saved_model 저장 완료")

# ④ onnx 변환
os.system(f'python -m tf2onnx.convert --saved-model "{save_path}" --output stock_model.onnx --opset 13')
print("onnx 저장 완료")