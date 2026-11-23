Theresa 조립 효과 — NormalParticle VFX 연결

구현만 수행했습니다. Unity 컴파일 확인, Play 모드 재생, 시각 및 성능 검증은 하지 않았습니다.

적용 방식
- 기존 TheresaFragmentAssembly 컴포넌트가 ParticleSystem 대신 VisualEffect를 생성합니다.
- Fragment Vfx Asset을 비워두면 아래 Resources의 조립 전용 VFX를 자동으로 불러옵니다.
- 기존 메인 장면과 미리보기에서 별도 재배치 없이 이 코드 경로를 사용합니다.
- 원본 Assets/Testing/Effeccts/NormalParticle.vfx는 수정하지 않았습니다.

조립용 복제본
Assets/TheresaAssembly/Resources/Theresa/NormalParticleAssembly.vfx
원본의 Shader Graph 출력, 텍스처, Orient, 크기 및 Alpha 곡선을 유지합니다.
연속 생성 대신 8192개 고정 풀을 한 번 생성하고 CPU 샘플에 고정 인덱스로 대응시킵니다.
사용하지 않는 인덱스와 도착한 조각은 크기/Alpha를 0으로 만듭니다.
완료 상태에서는 VFX Renderer도 숨깁니다. GPU 풀은 다음 재생을 위해 유지됩니다.

코드 → VFX 데이터
AssemblyData: Structured GraphicsBuffer<float4>, stride 16, 8192 * 3 요소
각 샘플당 3요소:
  0: 월드 위치 xyz / VFX 크기 배율 w
  1: 비행 진행도 x / 표시 여부 y
  2: 색상 rgba (그래프의 color/alpha 속성에 전달)
AssemblyBoundsCenter / AssemblyBoundsSize: VFX 로컬 좌표로 계산한 표시 범위
입력 데이터 반영 뒤 AdvanceOneFrame으로 수동 갱신합니다.
원래의 age/lifetime 기반 곡선에는 실제 경과 시간 대신 조각의 비행 진행도를 전달합니다.

조절
Vfx Size Multiplier: 원본 VFX 크기 곡선에 곱하는 배율, 기본 1
Vfx Tint: 입자 color/alpha 속성. 최종 색 적용은 Shader Graph의 color 사용 방식에 따릅니다.
Fragment Count: 표면 샘플 수, 기본 1600 (실행 중 변경 시 초기화 전까지 적용되지 않음)
Assembly Direction / Direction Space / Reverse Direction: 기존 3D 방향 설정
Cell Size 3D / Cell Delay: 본체의 셀 크기 및 시간차
Scatter Distance / Stream Spread: 출발 거리 및 흐름 퍼짐
Follow Reveal Direction을 끄면 Fragment Travel Direction 3D로 입자 방향을 분리합니다.
예: Assembly Direction (0,-1,0) = 위에서 들어와 아래쪽 순서로 조립.

기존 fragmentMaterial, fragmentMesh, fragmentSize, fragmentColor는 이전 장면 데이터 보존용으로만 남겼고
Inspector에서 숨겼습니다. 현재 VFX 외형에는 사용하지 않습니다.
Toon 셰이더와 표면 샘플링·조립 타이밍 계산은 이번 작업에서 변경하지 않았습니다.
사용자가 주석 처리한 미리보기 카메라 추적 코드도 유지했습니다.

주의할 편집 지점
- 조립용 그래프의 AssemblyRead / AssemblyVisibility 블록, AssemblyData 속성,
  고정 Burst 8192 / Capacity 8192를 임의로 변경하면 코드의 인덱스 규약도 같이 수정해야 합니다.
- 비활성 입자를 Kill하면 고정 인덱스 재사용이 깨지므로 size/alpha로 숨깁니다.
- 원본 그래프를 Fragment Vfx Asset에 직접 지정하면 AssemblyData 연결이 없으므로 사용할 수 없습니다.
- 애니메이션 표면 샘플 계산은 여전히 CPU입니다. 이번 변경은 렌더링 백엔드를 VFX Graph로 교체한 것입니다.

첨부 ZIP은 추가 구현 파일 백업입니다. 기존 프로젝트의 원본 Shader Graph/텍스처/모델/Toon 의존 파일은 포함하지 않습니다.
