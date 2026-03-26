using UnityEngine;
using Photon.Pun;

public class RoleManager : MonoBehaviourPun
{
    [Header("Role Prefabs")]
    [SerializeField] private GameObject hostPrefab;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject audiencePrefab;

    public GameObject GetRolePrefab(string role)
    {
        switch (role.ToLower())
        {
            case "host":
                return hostPrefab;
            case "audience":
                return audiencePrefab;
            default:
                return playerPrefab;
        }
    }

    public bool CanPerformAction(string action, string role)
    {
        switch (action)
        {
            case "startGame":
                return role == "host";
            case "answerQuestion":
                return role == "player";
            case "vote":
                return role == "audience" || role == "player";
            case "controlCamera":
                return role == "audience";
            default:
                return false;
        }
    }

    public string GetRoleDescription(string role)
    {
        switch (role.ToLower())
        {
            case "host":
                return "You are the Host! Control the game, ask questions, and manage the show.";
            case "player":
                return "You are a Contestant! Answer questions, complete challenges, and compete to win!";
            case "audience":
                return "You are in the Audience! Watch the action, vote, and influence the show!";
            default:
                return "Unknown role";
        }
    }
}