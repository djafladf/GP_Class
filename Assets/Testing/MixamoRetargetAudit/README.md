# Mixamo 리타게팅 진단 결과 — 부분 해결

검사: 2026-11-17, Unity 6000.3.27f1. 오후 6시 55분(KST), 마감 전에 부분 해결 상태로 작업을 중단했습니다.

## 판정
- `Copied Avatar Rig Configuration mis-match ... J_Bip_C_Hips ... not found`는 복제본에서 동일하게 재현했습니다.
- Mixamo 복제본을 Humanoid / Create From This Model / Source Avatar 없음으로 가져오면 유효한 자체 Avatar와 Humanoid 클립이 생성됩니다. 이 임포트 오류에 대한 수정은 확인했습니다.
- **전체 리타게팅 품질은 미해결입니다.** UnityChan에서 일어서는 동작은 재생되지만 후반 발 뜸이 남습니다. 실제 게임 적용이나 전환까지 검증한 해결본이 아닙니다.

## 프로젝트에서 확인할 위치
프로젝트: D:\Proj1\GP_Class
테스트 폴더: Assets/Testing/MixamoRetargetAudit
씬: StandingUp_Retarget_Test.unity
애니메이션 FBX: StandingUp_Fixed.fbx
캐릭터 FBX: UnityChan_Test.fbx
Controller: StandingUp_Test.controller

`Fixed`는 Avatar 임포트 수정만 뜻합니다. 최종 발 접지나 모든 관절 품질의 해결을 뜻하지 않습니다.
기존 Main 씬과 기존 Test.controller에는 적용하거나 저장하지 않았습니다. 작업 시작 시 Main 씬은 이미 저장되지 않은 변경이 있었습니다.

## 확인한 원인과 상태
1. Mixamo 실제 골반 본은 `mixamorig1:Hips`, am3 Avatar의 골반 본은 `J_Bip_C_Hips`입니다. Hips 역할이 같아도 서로의 실제 Transform 이름과 계층이 다릅니다. Mixamo FBX에 am3 Avatar를 Copy From Other로 지정하면 요청한 오류가 재현됩니다.
2. 작업 시작 시 원본 Standing Up은 이미 Create From This Model 상태였고 자체 Avatar가 valid/human이었습니다. 따라서 과거 오류 로그와 현재 설정을 구분했습니다. 원본 설정을 새로 고쳤다고 주장하지 않습니다.
3. 원본 세 모델의 매핑된 Transform 존재 여부를 검사했습니다. 자체 Avatar는 모두 유효했습니다. 저장된 Avatar 기준 자세를 적용해 팔 방향을 검사했으며 Mixamo 팔은 수평에서 약 2도 이내, UnityChan은 약 0.5도 이내였습니다. T-Pose를 무조건 다시 강제하지 않았습니다. 손가락 전체의 시각적 자세까지 보증하지 않습니다.
4. 기존 씬 UnityChan Animator는 UnityChan 자신의 Avatar를 사용했습니다. am3 Animator는 별도 am3_RightArm_IK.asset을 사용합니다. am3 복제본의 재임포트에서 스케일이 달라지는 문제가 발생하여 최종 동작 검증 대상은 UnityChan으로 제한했습니다. 이를 원본 am3의 결함으로 판정하지 않습니다.
5. 애니메이션은 0~182프레임, 30fps, 약 6.067초입니다. 시작은 다리를 앞으로 뻗고 바닥에 앉은 자세이며, 끝은 일어선 자세입니다. 무릎을 세우고 앉은 시작 자세와는 다릅니다. 반복 재생을 끈 단일 상태로 검증했습니다.

## Root Transform 검사
원본은 Rotation/Y/XZ Bake가 꺼져 있고 Y는 Original입니다.
복제본에서 Rotation=Original/Bake, Y=Original/Bake, XZ=Bake, Animator Apply Root Motion=false인 고정 위치 테스트를 구성했습니다.
Feet와 Y Bake 조합은 원본 발끝의 높이까지 낮추는 문제가 있어 최종 기준 설정으로 채택하지 않았습니다.

Root 설정 3종 × 캐릭터 3종 × Foot IK 켜기/끄기, 각 61시점을 비교한 기록이 있습니다. am3 수치는 복제 스케일 실험 영향이 있어 품질 판정에서 제외합니다.
UnityChan은 최종 기준 설정에서 끝 프레임 발목 본 높이가 약 0.313~0.315 Unity 단위입니다. 발목 높이는 신발 바닥 높이와 동일하지 않으나 시각적/본 자세 비교에서도 발 뜸이 남습니다. Foot IK나 Feet 설정만으로 해결됐다고 판단할 수 없습니다.

원본 Generic FBX와 자체 Avatar Humanoid를 61시점, 20개 본으로 비교했습니다. 회전 차이는 평균 약 11.55도, 최대 약 60.40도였습니다. 주요 관절 위치는 시작/끝에서 유사한 동작을 보이나, 이 회전 수치를 근거로 정확한 포즈 일치를 주장하지 않습니다. Twist 분배와 발끝 차이의 추가 분석이 필요합니다.

## 실패한 실험
Experimental_Unresolved 폴더의 높이 곡선 보정 클립/씬/프리팹은 실험 자료입니다. 파일명에 Grounded 또는 Verified가 있어도 **성공 판정이 아닙니다**. 샘플 간 검증에서 약 0.076 Unity 단위 오차가 남은 UnityChan 실험을 포함합니다. 게임에 사용하지 마세요.
자동 실행되는 검사 코드는 제거했으며, 남은 검사 도구는 메뉴에서 수동으로만 실행됩니다.

## 원본 보존
Mixamo Standing Up, UnityChan, am3_RightArm의 원본 FBX 및 .meta 총 6개는 작업 전후 SHA-256이 모두 같습니다.
백업은 이 작업 공간의 work/originals에 있습니다. 프로젝트의 기존 사용자 변경은 되돌리지 않았습니다.

## 남은 작업
- UnityChan 체형에 맞는 골반 높이와 발/발끝 IK 목표를 함께 조정하고, 바닥 접지와 무릎 방향을 실제 재생에서 확인.
- Generic 원본과 Humanoid의 Twist/발끝 회전 차이를 관절 위치·스킨 변형과 함께 분석.
- 기존 Sitting Idle의 끝 자세와 Standing Up 시작 자세를 맞추고 전환 시간/Foot IK/레이어 마스크를 검증.
- 기존 게임 스크립트, CharacterController, Rig/IK 제약이 포함된 실제 플레이 모드 검증.
- am3는 원본 Avatar/IK Avatar와 일관된 스케일의 복제본을 새로 준비한 뒤 별도 검증.

참고: Unity 공식 문서
- https://docs.unity.com/en-us/engine/6000.3/script-reference/unityeditor/modelimporteravatarsetup
- https://docs.unity.com/en-us/engine/6000.3/manual/animation-section/animation-mecanim/avatar-creationand-setup/root-motion
- https://docs.unity.com/en-us/engine/6000.3/manual/assets-and-media/asset-types/models/importing/configuringthe-avatar

