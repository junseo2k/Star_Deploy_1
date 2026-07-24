# DELUSION 홈 UI — 무문자 Unity 소스팩

이 압축에는 화면에 글자나 숫자가 구워진 PNG가 없습니다. 폰트 파일도 포함하지 않았습니다.
모든 메뉴명, 제목, 수치, 뉴스 문장, 버튼 문구는 Unity에서 TextMeshPro 오브젝트로 별도 배치해야 합니다.

## 바로 사용하면 되는 폴더
- `Assets/UI/Panels`: 실제 크기의 빈 사이드바·상태 바·카드·대사창
- `Assets/UI/Buttons`: 문구 없는 다음 날 버튼 3상태
- `Assets/UI/Pills`: 문구 없는 수익률·분류·건수 배지
- `Assets/UI/Icons`: 투명 PNG 아이콘
- `Assets/UI/NineSlice`: 크기 변경용 패널
- `Assets/Backgrounds`: UI·문자·캐릭터 없는 배경
- `Assets/Characters`: 리나 투명 PNG
- `Assets/Content/NewsThumbnails`: 뉴스 썸네일
- `Config/layout_reference.json`: 1920×1080 배치 좌표
- `Config/textmeshpro_slots.json`: TMP 권장 영역

## Unity 가져오기
- Texture Type: Sprite (2D and UI)
- Alpha Is Transparency: On
- Mesh Type: Full Rect
- Filter Mode: Bilinear
- Canvas Scaler: Scale With Screen Size / 1920×1080 / Match 0.5
- 텍스트는 전부 TextMeshPro로 생성

완성 화면을 통째로 넣은 목업 PNG와 `mockup_layers` 폴더는 의도적으로 제외했습니다.
각 PNG를 개별 UI 오브젝트로 배치해 사용하세요.
