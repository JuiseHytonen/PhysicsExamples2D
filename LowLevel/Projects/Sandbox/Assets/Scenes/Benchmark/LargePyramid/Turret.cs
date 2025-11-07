using UnityEngine;
using UnityEngine.LowLevelPhysics2D;

public class Turret
{
    private const float TURRET_ROTATION_SPEED = .030f;
    private PhysicsBody m_Body;
    public Turret(float posX, float posY)
    {
           var capsuleRadius = 1;
            var capsuleLength = capsuleRadius * 6;
            var capsuleGeometry = new CapsuleGeometry
            {
                center1 = Vector2.zero,
                center2 = Vector2.right * capsuleLength,
                radius = capsuleRadius,
            };

            var bodyDef = new PhysicsBodyDefinition { bodyType = RigidbodyType2D.Kinematic, gravityScale = 0, fastCollisionsAllowed = false };
            var shapeDef = new PhysicsShapeDefinition
            {
                contactFilter = new PhysicsShape.ContactFilter { categories = 0, contacts =  0 },
                surfaceMaterial = new PhysicsShape.SurfaceMaterial { friction = 0.0f, bounciness = 0.3f },
                contactEvents = true
            };

            bodyDef.position = new Vector2(posX, posY);
            bodyDef.rotation = new PhysicsRotate(0f);



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

    public Vector2 GetRotation()
    {
        return m_Body.rotation.direction;
    }

    public Vector2 GetPosition()
    {
        return m_Body.position;
    }

}
