using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;

[UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
public unsafe partial struct PhysUpdate : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        GameManager gm = GameManager.instance;
        if (SystemAPI.HasSingleton<PhysicsStep>())
        {
            var physicsStep = SystemAPI.GetSingletonRW<PhysicsStep>();
            physicsStep.ValueRW.Gravity = new float3(gm.gravityX, gm.gravityY, gm.gravityZ);
            physicsStep.ValueRW.SolverIterationCount = gm.solverIterations;
            physicsStep.ValueRW.SimulationType = SimulationType.HavokPhysics;
        }
    }
}
