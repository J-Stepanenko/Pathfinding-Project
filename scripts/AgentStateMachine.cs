
public static class AgentStateMachine
{
    public enum AgentStates
    {
        Attacking, // When enemy is in range
        Chasing, // No enemies in range
        Forming_up, // Getting into a formation
        Retreating // Retreating away from the enemy
    }

}
