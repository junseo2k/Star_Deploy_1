import tensorflow as tf
from tensorflow.keras.datasets import imdb #데이터셋과 텐서플로우, 케라스 import
import numpy as np
vocab_size = 50000 #단어 사전 크기를 제한
(x_train,y_train),(x_test,y_test) = tf.keras.datasets.imdb.load_data(num_words = vocab_size) #tensorflow에 내장된 IMDB 영화 리뷰 데이터셋(상위 50000개)을 불러옴.
#load 함수가 데이터들을 다 훈련용/테스트용으로 분류한 후 반환함.
max_review_length = 500
x_train = tf.keras.preprocessing.sequence.pad_sequences(x_train, maxlen = max_review_length)
x_test = tf.keras.preprocessing.sequence.pad_sequences(x_test, maxlen = max_review_length) #패딩
word_index = tf.keras.datasets.imdb.get_word_index()#인덱스를 다시 단어로 변환하여 사람이 이해할 수 있도록 변환
reverse_word_index = dict([value, key] for (key, value) in word_index.items())
def decode_review(padded_sequences):
    return " ".join([reverse_word_index.get(i - 3, "?") for i in padded_sequences]) #데이터셋의 단어들의 키-값을 역전시킨 후, 사전에서 그 번호에 해당하는 단어를 넣음. 못찾으면 ?를 넣음. 이 단어들을 공백으로 이어붙여서 하나의 문장으로 만듬
model = tf.keras.models.Sequential() # 빈 스택 정의. 텍스트에서만 사용
model.add(
    tf.keras.layers.Embedding(
        input_dim= vocab_size,
        output_dim= 50,
        input_length= max_review_length
    )
)
model.add( #드롭다운 층. 25%를 무작위로 비활성화
    tf.keras.layers.Dropout(
        rate=0.25
    )
)
model.add( #LSTM 추가. 32개의 유닛을 사용. 유닛은 층 안에 있는 뉴런 개수
    tf.keras.layers.LSTM(
        units = 32
    )
)
model.add( #드롭다운 층. 25%를 무작위로 비활성화. 과적합 방지를 위해 하나 더 추가
    tf.keras.layers.Dropout(
        rate=0.25
    )
)
model.add(
    tf.keras.layers.Dense(
        units = 1,
        activation="sigmoid"
    )
)
model.compile(
    loss = tf.keras.losses.binary_crossentropy,
    optimizer=tf.keras.optimizers.Adam(),
    metrics = ["accuracy"]
)
model.build(input_shape=(None, max_review_length))
model.summary()

history = model.fit(
    x_train, y_train,
    batch_size = 256,
    epochs = 3,
    validation_split = 0.2, #테스트 데이터 분할
    verbose = 1
)
predicted_probs = model.predict(x_test)
predicted_classes_reshaped = (predicted_probs > 0.5).astype("int32").reshape(-1)
incorrect = np.nonzero(predicted_classes_reshaped != y_test)[0]
print(f"테스트 정확도: {np.mean(predicted_classes_reshaped == y_test):.4f}")
print(f"틀린 개수: {len(incorrect)} / {len(y_test)}")