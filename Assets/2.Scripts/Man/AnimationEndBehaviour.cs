using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class AnimationEndBehaviour : StateMachineBehaviour
{
    [SerializeField] string StartfunctionName; // 실행할 함수명
    [SerializeField] string EndfunctionName;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if(StartfunctionName != "") animator.gameObject.SendMessage(StartfunctionName, SendMessageOptions.DontRequireReceiver);
    }

    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (EndfunctionName != "") animator.gameObject.SendMessage(EndfunctionName, SendMessageOptions.DontRequireReceiver);
    }

}