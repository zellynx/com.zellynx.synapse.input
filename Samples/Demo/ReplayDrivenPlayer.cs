using Synapse.Runtime.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameInputSystem.Demo
{
    public class ReplayDrivenPlayer : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private MonoBehaviour inputReaderSource;

        [SerializeField] private InputActionReference moveAction;

        [SerializeField] private float moveSpeed = 5f;

        private IInputReader inputReader;

        private void Awake()
        {
            inputReader =
                inputReaderSource as IInputReader;

            if (inputReader == null)
            {
                Debug.LogError(
                    "[ReplayDrivenPlayer] Invalid Input Reader."
                );
            }
        }

        private void Update()
        {
            Vector2 move =
                inputReader
                    .ReadValue<Vector2>(
                        moveAction.action)
                    .Value;

            Vector3 movement =
                new(move.x, 0f, move.y);

            transform.position +=
                movement * (moveSpeed * Time.deltaTime);
        }
    }
}