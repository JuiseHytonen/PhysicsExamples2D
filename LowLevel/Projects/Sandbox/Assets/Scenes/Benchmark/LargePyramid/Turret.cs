using UnityEngine;
using UnityEngine.LowLevelPhysics2D;

public class Turret
{
    private const float TURRET_ROTATION_SPEED = .030f;
    private PhysicsBody m_Body;
    public Turret(float posX, float posY)
    {
           var capsuleRadius = 1;
            var capsuleLength = capsuleRadius * 4;
            var capsuleGeometry = new CapsuleGeometry
            {
                center1 = Vector2.left * capsuleLength,
                center2 = Vector2.right * capsuleLength,
                radius = capsuleRadius
            };

            var bodyDef = new PhysicsBodyDefinition { bodyType = RigidbodyType2D.Dynamic, gravityScale = 0, fastCollisionsAllowed = true };
            var shapeDef = new PhysicsShapeDefinition
            {
                //contactFilter = new PhysicsShape.ContactFilter { categories = m_ProjectileMask, contacts =  m_DestructibleMask | m_GroundMask },
                surfaceMaterial = new PhysicsShape.SurfaceMaterial { friction = 0.0f, bounciness = 0.3f },
                contactEvents = true
            };

            bodyDef.position = new Vector2(posX, posY);
            bodyDef.rotation = new PhysicsRotate(2f);



            m_Body = PhysicsWorld.defaultWorld.CreateBody(bodyDef);

            m_Body.callbackTarget = this;
            var shape =  m_Body.CreateShape(capsuleGeometry, shapeDef);
            shape.callbackTarget = this;
    }

    public void RotateRight()
    {
        m_Body.rotation = m_Body.rotation.Rotate(TURRET_ROTATION_SPEED);
    }

    public void RotateLeft()
    {
        m_Body.rotation = m_Body.rotation.Rotate(-TURRET_ROTATION_SPEED);
    }

}
