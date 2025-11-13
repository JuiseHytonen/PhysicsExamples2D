public class ExplodeTool
{
    public static void Explode()
    {
        /**
        if (beginEvent.shapeA.GetDensity() > 99)
        {
            beginEvent.shapeA.Destroy();
        }
        else
        {
            beginEvent.shapeB.Destroy();
        }

        return;
        const float radius = 10f;
        PhysicsWorld.defaultWorld.DrawCircle(beginEvent.shapeB.transform.position, radius, Color.orangeRed, 0.09f, PhysicsWorld.DrawFillOptions.All);
        var explodeDef = new PhysicsWorld.ExplosionDefinition { position = beginEvent.shapeB.transform.position, radius = radius, falloff = 2f, impulsePerLength = 90f };

        // Explode in all the worlds.
        using var worlds = PhysicsWorld.GetWorlds();
        foreach (var world in worlds)
            world.Explode(explodeDef);
        if (beginEvent.shapeA.body.bodyType != RigidbodyType2D.Static)
        {
            beginEvent.shapeA.Destroy();
        }
        if (beginEvent.shapeB.body.bodyType != RigidbodyType2D.Static)
        {
            beginEvent.shapeB.Destroy();
        }
        **/
    }
}
