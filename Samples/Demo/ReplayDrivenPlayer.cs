using Synapse.Input.Reader;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameInputSystem.Demo
{
    public class ReplayDrivenPlayer : MonoBehaviour
    {
        [SerializeField] private IInputReader inputReader;

        [SerializeField] private InputActionReference moveAction;

        [SerializeField] private float moveSpeed = 5f;

        private void Awake() {
            if (inputReader == null) {
                Debug.LogError("[ReplayDrivenPlayer] Invalid Input Reader.");
            }
        }

        private void Update() {
            Vector2 move = inputReader.ReadValue<Vector2>(moveAction.action).Value;
            Vector3 movement = new(move.x, 0f, move.y);

            transform.position += movement * (moveSpeed * Time.deltaTime);
        }
    }
}