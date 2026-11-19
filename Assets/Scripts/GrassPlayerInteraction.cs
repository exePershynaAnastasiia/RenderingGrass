
using UnityEngine;

public class GrassPlayerInteraction : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform player;

    private static readonly int PlayerPositionID =
        Shader.PropertyToID("_PlayerPositionWS");

    private static readonly int PlayerForwardID =
    Shader.PropertyToID("_PlayerForwardWS");

    private void LateUpdate()
    {
        if (player == null)
            return;

        // Send the player's world position to all grass shaders
        Shader.SetGlobalVector(
            PlayerPositionID,
            new Vector4(
                player.position.x,
                player.position.y,
                player.position.z,
                1f
            )
        );

        // Send the player's forward direction to the grass shader
        Vector3 forward = player.forward;

        Shader.SetGlobalVector(
            PlayerForwardID,
            new Vector4(forward.x, forward.y, forward.z, 0f)
        );
            }
}
