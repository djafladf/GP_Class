# Unity 6.3 양손 롱소드 배치 도구

원본 TH_Idle, Controller, 프리팹, 씬 파일을 자동으로 수정하지 않습니다. Tools 메뉴에서 선택한 Animator 전체를 **작업용 복제본**으로 만든 후 그 복제본에만 배치합니다. 복제본을 씬에 저장하는 것은 사용자가 결정합니다.

## 프로젝트에서 확인한 내용

- Unity 6000.3.27f1 / Animation Rigging 1.4.1.
- 기준 클립: `Assets/Testing/Anim/RigTest/TH_Idle.anim`.
- TH_Idle은 길이 0의 정적 **Weapon Transform 포즈**입니다. 위치 `(-0.06, 1.35, 0.25)`, Euler `(84, 370, 110)`이며, 손·팔 본이나 Humanoid muscle 곡선은 없습니다. 이 숫자를 새로운 기본값으로 하드코딩하지 않고 실제 클립을 샘플링합니다.
- `Main.unity`에 `Pl/unitychan/Weapon`, `Weapon/RGrip`, `Weapon/LGrip`, `Rigs/RIK`, `Rigs/LIK`이 있습니다. unitychan과 Weapon에 각각 균일한 1.2 스케일이 있습니다. 월드/로컬 변환에 이를 반영합니다.
- 기존 RIK/LIK의 Tip은 실제 Hand 본이 아니라 Hand 아래 RGrip/LGrip 보조 오브젝트입니다. 새 IK는 Avatar의 UpperArm → LowerArm → **Hand**를 사용합니다.
- 기본 Controller의 Idle은 `Moving/WAIT00.anim`을 참조하지만, 이것이 원하는 양손 팔 자세라는 근거는 없습니다. 자동 지정하지 않습니다.

**따라서 TH_Idle만으로 원하는 양손 자세를 복구할 수는 없습니다.** 함께 보던 몸 애니메이션을 Body Idle에 지정하거나, 작업 복제본의 손·팔이 원하는 자세인 것을 확인한 뒤 Use Current Body Pose를 선택해야 합니다. 원본 프로젝트 자료는 확인했지만 Unity 화면에서 실제 검 메시 방향·최종 접촉 상태까지 검증한 것은 아닙니다.

## 설치 / 사용

1. 이 폴더의 Runtime/Editor를 프로젝트 `Assets/TwoHandLongsword` 아래에 둡니다. Animation Rigging 1.4.1이 이미 설치되어 있으므로 패키지 변경은 필요 없습니다. 기존 사용자 asmdef 폴더 아래에 넣지 마세요.
2. Play Mode와 Animation 창 Preview를 종료합니다. Hierarchy에서 **Animator가 붙은 unitychan 오브젝트 자체**를 선택합니다. 바깥 Pl 오브젝트를 선택하지 않습니다.
3. `Tools > Two Hand Longsword > Create Working Copy From Selected Animator`를 실행합니다. 원본 옆에 `_TwoHandWorkingCopy`가 생깁니다. 게임 로직과 Collider는 복제본에서 비활성화합니다. 원본과 겹쳐 보이면 Scene Visibility로 원본을 숨기세요.
4. 복제본 Inspector의 `Two Hand Longsword Setup`에서 Reference Idle은 TH_Idle, Sword는 **복제본의 Weapon Transform**인지 확인합니다. 이 프로젝트 경로는 자동 입력합니다.
5. Body Idle에 원하는 양손 몸 자세 클립을 넣고 Body Time을 정합니다. 현재 보이는 손·팔 자세를 쓰려면 Body Idle을 비우고 Use Current Body Pose를 켭니다. 일반 WAIT00이 원하는 양손 자세인지 먼저 확인하세요.
6. **Blade Direction Local**은 Weapon의 로컬 좌표에서 폼멜 → 검끝 방향입니다. Scene의 Local 축과 실제 메시를 비교해 입력합니다. 예: +Z이면 `(0,0,1)`, -Z이면 `(0,0,-1)`. 이 프로젝트의 축을 임의로 추정하지 않도록 초기값은 0입니다. 잘못된 축/참조로 오른손이 왼손보다 아래에 오면 배치를 중단합니다.
7. `1. Capture TH_Idle and Build Controls`를 누릅니다. 기존 Weapon 경로는 유지하고 그 시각 메시를 독립 Sword로 복제합니다. 기존 Weapon의 Renderer만 작업 복제본에서 숨깁니다. 원본 TH_Idle의 곡선 연결은 유지됩니다.
8. `3. Start Edit Mode IK Preview`를 누르면 오른쪽 약 2m에 일시적인 IK 미리보기 복제본이 생깁니다. Inspector에서 Offset을 바꾸고 `Refresh Preview With Offsets`를 누르세요. 원본 작업 복제본과 미리보기를 비교할 수 있습니다.
9. Idle 외에는 Preview Body에 Walk 또는 Run과 Preview Time을 넣어 미리보기를 **종료 후 다시 시작**합니다. 이는 지정 시간의 정적 샘플입니다. 타임라인 재생/스크럽이나 실시간 연속 갱신은 제공하지 않습니다.
10. `Stop Preview` 후 `2. Apply Inspector Offsets`를 눌러 작업 복제본에 확정합니다. 배치와 Transform 변경은 Undo가 가능합니다. 재캡처는 원본에서 새 작업 복제본을 만드세요. 기존 baseline을 덮어쓰지 않습니다.

생성 계층:

```text
unitychan_TwoHandWorkingCopy (Animator + RigBuilder + Setup)
├─ Character1_Reference/.../RightHand, LeftHand
├─ Weapon                       기존 TH_Idle 바인딩 보존, 시각 메시 숨김
├─ Rigs                         기존 구성 참고용 보존, 새 Builder 목록에서는 제외
├─ WeaponControl                어느 손/척추 본에도 종속되지 않음
│  └─ Sword                     TH_Idle의 검 월드 포즈 보존
│     ├─ RightGripTarget        손목 목표 위치 + 실제 Hand 본 회전
│     └─ LeftGripTarget
├─ RightElbowHint
├─ LeftElbowHint
└─ TwoHandLocomotionRig
   ├─ RightArmIK
   └─ LeftArmIK
```

## 계산 / Offset

TH_Idle과 몸 Idle을 일회용 복제본에 샘플링합니다. 양 손목의 중간점을 WeaponControl 원점으로 삼고, Sword에는 그 중간점으로부터의 상대 위치와 기존 검 회전을 저장합니다. **검 메시를 중간점으로 강제로 옮기지는 않습니다.** 따라서 제어 중심은 양손 사이에 생기면서 기존 TH_Idle 검 배치는 보존됩니다. 이후 캐릭터 루트의 자식으로 고정되어 Idle/Walk/Run에서 앞쪽 상대 위치를 유지합니다. LateUpdate 추적은 없습니다.

각 손목의 위치·회전을 Sword 로컬 공간으로 변환하여 목표를 만듭니다. 서로 다른 본 축을 가정하여 좌우에 똑같은 Euler 회전을 주지 않습니다. 이 Target은 손목 IK용이므로 **손바닥 접촉점/손가락의 시각적인 중심과 다를 수 있습니다.** 손잡이 접촉은 Position/Rotation Offset으로 미세 조정합니다.

Hint는 어깨→손목 축에 대해 팔꿈치가 벗어난 성분을 구하고, 그 방향으로 실제 팔 길이 × Hint Distance만큼 확장합니다. 완전히 편 팔처럼 굽힘 평면이 없으면 캐릭터 좌우 방향을 투영하여 시작값을 만듭니다.

- Weapon Position Offset: 캐릭터 루트 로컬 단위.
- Weapon Rotation Offset: 양손 중간점을 중심으로 회전. 기준 검 회전에 상대적으로 적용됩니다.
- Right/Left Position Offset: Sword 로컬 단위. Sword의 균일 스케일이 적용됩니다.
- Right/Left Rotation Offset: 캡처된 각 손목 회전 뒤에 곱하는 Euler 보정.
- Hint Offset: 캐릭터 루트 로컬 단위.
- Hint Distance: 캡처 시만 사용. 캡처 후에는 Hint Offset으로 조정합니다.

## 이동 / 공격 연동

생성 Rig는 기본 Weight 0입니다. 작업 복제본의 게임 로직이 꺼져 있으므로 테스트용 구성이 즉시 플레이어를 대체하지 않습니다. 원하는 구성을 새 프리팹으로 정리하고 실제 게임 코드에서 이동용 상태 진입 시 아래 메서드를 호출하세요.

```csharp
// Idle / Walk / Run 상태에 진입할 때
twoHandSetup.SetLocomotionActive(true);

// 공격에 진입하기 전: 양팔 Rig를 즉시 0으로 해제
twoHandSetup.SetLocomotionActive(false);
```

이동 진입은 Blend Speed로 섞습니다. 공격 여부를 이름으로 자동 추측하지 않습니다. 전환 중 공격에도 IK가 남지 않도록 공격 시작 전에 해제 호출이 필요합니다. 이 도구는 공격 때의 검 부착/검 궤적을 구현하지 않습니다. 기존 공격용 Weapon이나 무기 부착 시스템으로 표시를 전환하는 로직은 별도로 연결하세요. 무기 표시 전환 없이 사용하면 독립 Sword는 계속 이동용 위치에 남습니다.

## 검증과 한계

- 설치 전 Unity 6000.3.27f1의 실제 엔진/Editor DLL과 프로젝트 Animation Rigging DLL에 대해 C# 컴파일 검사를 수행했습니다. 결과는 VALIDATION.ko.md에 기록합니다.
- Unity Editor 내 Avatar 샘플링, PlayableGraph 평가, 메시 접촉, Play Mode 상태 전환까지 실행 검증했다고 주장하지 않습니다. 최종 자세는 위 미리보기로 확인해야 합니다.
- Humanoid Avatar가 필요합니다. Generic은 임의의 본 이름으로 추측하지 않고 중단합니다.
- 비균일/음수 스케일은 중단합니다. 균일 스케일은 지원합니다.
- 검끝 축·손잡이의 실제 길이·손가락 접촉·가드 충돌·손목 twist 한계는 자동 인식하지 않습니다. 메시의 긴 축만으로 손잡이를 확정할 수 없습니다.
- 참조 팔 자세가 없으면 검 위치만으로 손목 회전을 복원할 수 없습니다. 기존 컨트롤러의 레이어/마스크/IK 평가 결과까지 캡처하는 기능은 없습니다.
- 팔 길이 밖 Target은 Two Bone IK가 도달할 수 없습니다. Walk/Run의 어깨 이동, 루트모션, Avatar 리타기팅, Animator culling, 다른 IK와의 충돌은 실제 게임에서 확인해야 합니다.
- TH_Idle 검 곡선은 원본 Weapon에 남아 있고 독립 Sword는 캡처된 위치를 사용합니다. 이동 중 검의 추가 움직임이 필요하면 WeaponControl을 별도로 애니메이션해야 합니다.
- 손 순서 검사는 캡처 때만 수행합니다. Offset으로 이후 손 순서를 뒤집거나 몸 뒤로 옮길 수 있으므로 최종 상태를 확인하세요.
- 동적으로 흔들리는 검/두 손 균형 솔버, 손가락 IK, 자동 손잡이 검출, 공격 보정은 포함하지 않습니다.

API 근거: [RigBuilder](https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.3/api/UnityEngine.Animations.Rigging.RigBuilder.html), [TwoBoneIKConstraint](https://docs.unity.cn/Packages/com.unity.animation.rigging@1.3/api/UnityEngine.Animations.Rigging.TwoBoneIKConstraint.html). 실제 설치된 1.4.1 소스에서도 Build/Evaluate와 데이터 필드를 대조했습니다.
