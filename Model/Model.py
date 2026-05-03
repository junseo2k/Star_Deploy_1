import tensorflow as tf
import numpy as np
import pandas as pd #csv 읽기 라이브러리. csv 파일은 각 열을 쉼표로 구분하는 텍스트 파일로, 엑셀과 거의 유사
df = pd.read_csv("stock_history.csv") #csv 파일을 표로 읽음. df는 데이터프래임
# 주식 하나 선택 (실제 게임 주식 이름으로 바꿔요)
# 슬라이딩 윈도우로 X, y 만들기
window_size = 10#과거 10일치를 보고 내일을 예측
X, y = [], [] #입력 데이터(x, 과거 10일치)와 정답 데이터(y, 내일 변동률) 리스트 생성
for stock_name in df["stockName"].unique():  # 모든 주식 이름을 중복 없이 하나씩 순회. unique가 중복 제거
    stock_df = df[df["stockName"] == stock_name].sort_values("day") #이 주식데이터만 필터링하고 날짜 순 정렬
    prices = stock_df["currentPrice"].values #가격만 뽑음
    change_pct = stock_df["priceChangePct"].values      # 변동률
    is_affected = stock_df["isAffectedByNews"].values   # 뉴스 영향 여부
    #가격 정규화. LSTM은 숫자에 민감하기 때문에 0~1사이 값으로 바꿔주는 게 좋음. 1일수록 가격이 높다고 판단    
    price_min = prices.min()
    price_max = prices.max()
    if price_max == price_min:  # 가격 변동이 없으면 건너뜀
        continue
    prices_normalized = (prices - price_min) / (price_max - price_min)
    for i in range(window_size, len(prices_normalized)): #10번째 날부터 끝까지 반복
    # 가격 + 소형뉴스 + 대형뉴스 3개를 묶어서 입력
        window = [] #이번 반복에서 만들 입력 데이터 10일치
        for j in range(i - window_size, i): #최근 과거 10일 동안 반복
            window.append([
                prices_normalized[j],  # 가격
                change_pct[j],         # 변동률
                is_affected[j]         # 뉴스 영향 여부
            ])
        X.append(window) #10일치 데이터를 x에 넣음. 가격, 변동률, 뉴스 영향 여부
        y.append(change_pct[i])#실제 어떤 변동이 있었는지 변동값을 y에 넣음. 이게 정답 데이터.

X = np.array(X) #리스트를 numpy 배열로 변환
y = np.array(y)

# train / test 나누기
split = int(len(X) * 0.8) #80%를 학습, 20%를 테스트로 나눔.
x_train, x_test = X[:split], X[split:] #train엔 처음부터 80%, test엔 80% 이후
y_train, y_test = y[:split], y[split:]

print(f"훈련: {len(x_train)}개 / 테스트: {len(x_test)}개") #데이터가 잘 나뉘었는지 확인
model = tf.keras.models.Sequential() # 빈 스택 정의. 텍스트에서만 사용. sequential = 순차적으로
model.add( #드롭다운 층. 25%를 무작위로 비활성화
    tf.keras.layers.Dropout(
        rate=0.25
    )
)
model.add( #LSTM 추가. 32개의 유닛을 사용. 유닛은 층 안에 있는 뉴런 개수
    tf.keras.layers.LSTM(
        units = 32,
        return_sequences=True  # 10일치 각각의 결과를 다음 LSTM에 넘겨줌. 없으면 마지막 날 결과만 넘겨줌
    )
)
model.add(tf.keras.layers.LSTM(units=32))#첫 번째 LSTM의 결과를 받아서 더 깊게 학습
model.add( #드롭다운 층. 25%를 무작위로 비활성화. 과적합 방지를 위해 하나 더 추가
    tf.keras.layers.Dropout(
        rate=0.25
    )
)
model.add(
    tf.keras.layers.Dense(#출력층.
        units = 1,#숫자 하나 출력(변동층)
        activation="linear"#출력값 제한 없음
    )
)
model.compile( 
    loss = "mse",#평균 제곱 오차로, 예측값과 실제값 차이를 제곱해서 평균냄.
    optimizer=tf.keras.optimizers.Adam(), #학습 방향을 조정하는 알고리즘으로, Adam이 가장 많이 쓰이는 옵티마이저
    metrics = ["mae"] #평균 절대 오차. 학습 중 얼마나 틀리는지 보여주는 지표
)
model.build(input_shape=(None, window_size, 3)) #모델에 입력 형태 알려주기. 샘플 수는 None으로, 상관 없음. 특성은 3개
model.summary()#모델 구조를 표로 출력

history = model.fit(
    x_train, y_train, #학습 데이터
    batch_size = 256, #한번에 256개씩 묶어서 학습
    epochs = 20, #전체 데이터를 20번 반복
    validation_split = 0.2, #테스트 데이터 분할. 20%가 검증용
    verbose = 1 #학습 진행상황 출력
)
predicted = model.predict(x_test) #테스트 데이터로 예측
predicted = predicted.reshape(-1) #2차원 배열을 1차원으로. [[2.3], [-1.5]] → [2.3, -1.5]

# 방법 B — 평균 오차
mae = np.mean(np.abs(predicted - y_test)) #예측값과 실제값 차이의 절댓값 평균을 냄. abs가 절댓값.
print(f"평균 예측 오차: {mae:.4f}%")

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