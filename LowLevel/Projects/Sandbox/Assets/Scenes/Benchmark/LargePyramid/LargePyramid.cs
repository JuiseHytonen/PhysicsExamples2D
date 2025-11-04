using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.LowLevelPhysics2D;
using UnityEngine.UIElements;

public class LargePyramid : MonoBehaviour,  PhysicsCallbacks.IContactCallback
{
    private readonly PhysicsMask m_GroundMask = new(1);
    private readonly PhysicsMask m_DestructibleMask = new(2);
    private readonly PhysicsMask m_ProjectileMask = new(3);


    private SandboxManager m_SandboxManager;
    private SceneManifest m_SceneManifest;
    private UIDocument m_UIDocument;
    private CameraManipulator m_CameraManipulator;
    private PhysicsShape.ContactFilter m_DestructibleContactFilter;

    private int m_BaseCount;
    private Vector2 m_OldGravity;
    private float m_GravityScale;

    private void OnEnable()
    {
        m_SandboxManager = FindFirstObjectByType<SandboxManager>();
        m_SceneManifest = FindFirstObjectByType<SceneManifest>();
        m_UIDocument = GetComponent<UIDocument>();
        m_SandboxManager.SceneOptionsUI = m_UIDocument;

        m_CameraManipulator = FindFirstObjectByType<CameraManipulator>();
        m_CameraManipulator.CameraSize = 80f;
        m_CameraManipulator.CameraPosition = new Vector2(0f, 79f);

        // Set up the scene reset action.
        m_SandboxManager.SceneResetAction = SetupScene;

        m_BaseCount = 90;
        m_OldGravity = PhysicsWorld.defaultWorld.gravity;
        m_GravityScale = 2f;
        var world = PhysicsWorld.defaultWorld;
        world.autoContactCallbacks = true;

        SetupOptions();

        SetupScene();
    }

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            Shoot();
        }
    }

    public void OnContactBegin2D(PhysicsEvents.ContactBeginEvent beginEvent)
    {
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

    }


    public void OnContactEnd2D(PhysicsEvents.ContactEndEvent endEvent)
    {

    }

    private void Shoot()
    {
              ref var random = ref m_SandboxManager.Random;

              var capsuleRadius = 1;
            var capsuleLength = capsuleRadius;
            var capsuleGeometry = new CapsuleGeometry
            {
                center1 = Vector2.left * capsuleLength,
                center2 = Vector2.right * capsuleLength,
                radius = capsuleRadius
            };

            var bodyDef = new PhysicsBodyDefinition { bodyType = RigidbodyType2D.Dynamic, gravityScale = m_GravityScale, fastCollisionsAllowed = true };
            var shapeDef = new PhysicsShapeDefinition
            {
                //contactFilter = new PhysicsShape.ContactFilter { categories = m_ProjectileMask, contacts =  m_DestructibleMask | m_GroundMask },
                surfaceMaterial = new PhysicsShape.SurfaceMaterial { friction = 0.0f, bounciness = 0.3f },
                contactEvents = true
            };

            // Fire all the projectiles.
            var definitions = new NativeArray<PhysicsBodyDefinition>(1, Allocator.Temp);
            for (var i = 0; i < 1; ++i)
            {
                // Calculate the fire spread.
                var halfSpread = 1 * 0.5f;
                var fireDirection = new Vector2(1, 0.3f);
                var fireSpeed = 500f;

                // Create the projectile body.
                bodyDef.position = new Vector2(-100, 4);
                bodyDef.rotation = new PhysicsRotate(random.NextFloat(-3f, 3f));
                bodyDef.linearVelocity = fireDirection * fireSpeed;

                definitions[i] = bodyDef;
            }

            // Create the bodies.
            using var bodies = PhysicsWorld.defaultWorld.CreateBodyBatch(definitions);

            // Create the capsules.
            for (var i = 0; i < 1; ++i)
            {
                // Create the projectile shape.
                shapeDef.surfaceMaterial.customColor = m_SandboxManager.ShapeColorState;
                var body = bodies[i];
                body.callbackTarget = this;
               var shape =  body.CreateShape(capsuleGeometry, shapeDef);
               shape.callbackTarget = this;
            }

            // Dispose.
            definitions.Dispose();
    }

    private void OnDisable()
    {
        // Get the default world.
        var world = PhysicsWorld.defaultWorld;

        world.gravity = m_OldGravity;
    }

    private void SetupOptions()
    {
        var root = m_UIDocument.rootVisualElement;

        {
            // Menu Region (for camera manipulator).
            var menuRegion = root.Q<VisualElement>("menu-region");
            menuRegion.RegisterCallback<PointerEnterEvent>(_ => ++m_CameraManipulator.OverlapUI);
            menuRegion.RegisterCallback<PointerLeaveEvent>(_ => --m_CameraManipulator.OverlapUI);

            // Base Count.
            var baseCount = root.Q<SliderInt>("base-count");
            baseCount.value = m_BaseCount;
            baseCount.RegisterValueChangedCallback(evt =>
            {
                m_BaseCount = evt.newValue;
                SetupScene();
            });

            // Gravity Scale.
            var gravityScale = root.Q<Slider>("gravity-scale");
            gravityScale.value = m_GravityScale;
            gravityScale.RegisterValueChangedCallback(evt =>
            {
                m_GravityScale = evt.newValue;

                // Get the default world.
                var world = PhysicsWorld.defaultWorld;

                world.gravity = m_OldGravity * m_GravityScale;
            });

            // Fetch the scene description.
            var sceneDescription = root.Q<Label>("scene-description");
            sceneDescription.text = $"\"{m_SceneManifest.LoadedSceneName}\"\n{m_SceneManifest.LoadedSceneDescription}";
        }
    }

    private void SetupScene()
    {
        // Reset the scene state.
        m_SandboxManager.ResetSceneState();

        // Get the default world.
        var world = PhysicsWorld.defaultWorld;

        // Ground.
        {
            var groundBody = world.CreateBody(new PhysicsBodyDefinition { position = new Vector2(0f, -1f), bodyType = RigidbodyType2D.Static});

            var shapeDef = new PhysicsShapeDefinition
            {
              //  contactFilter = new PhysicsShape.ContactFilter { categories = m_GroundMask, contacts = m_ProjectileMask | m_DestructibleMask },
            };
            const float groundLength = 1000f;
            groundBody.CreateShape(PolygonGeometry.CreateBox(new Vector2(groundLength, 2f)), shapeDef);
            groundBody.CreateShape(PolygonGeometry.CreateBox(new Vector2(groundLength, 2f), radius: 0f, new PhysicsTransform(new Vector2(0f, groundLength), PhysicsRotate.identity)), shapeDef);
            groundBody.CreateShape(PolygonGeometry.CreateBox(new Vector2(2f, groundLength), radius: 0f, new PhysicsTransform(new Vector2(groundLength * -0.5f, groundLength * 0.5f), PhysicsRotate.identity)), shapeDef);
            groundBody.CreateShape(PolygonGeometry.CreateBox(new Vector2(2f, groundLength), radius: 0f, new PhysicsTransform(new Vector2(groundLength * 0.5f, groundLength * 0.5f), PhysicsRotate.identity)), shapeDef);
        }

        // Pyramid.
        {
            var bodyDef = new PhysicsBodyDefinition { bodyType = RigidbodyType2D.Dynamic };
            var shapeDef = new PhysicsShapeDefinition
            {
               // contactFilter = new PhysicsShape.ContactFilter { categories = m_DestructibleMask, contacts = m_ProjectileMask | m_GroundMask },
                contactEvents = true
            };

            const float halfHeight = 0.5f;
            const float radius = 0.05f;
            var boxGeometry = PolygonGeometry.CreateBox(new Vector2(halfHeight - radius, halfHeight - radius) * 2f, radius);

            const float shift = 1.0f * halfHeight;

            for (var i = 0; i < m_BaseCount; ++i)
            {
                var y = (2.0f * i + 1.0f) * shift;

                for (var j = i; j < m_BaseCount; ++j)
                {
                    var x = (i + 1.0f) * shift + 2.0f * (j - i) * shift - halfHeight * m_BaseCount;

                    bodyDef.position = new Vector2(x, y);
                    var body = world.CreateBody(bodyDef);

                    shapeDef.surfaceMaterial.customColor = m_SandboxManager.ShapeColorState;
                    body.CreateShape(boxGeometry, shapeDef);
                }
            }
        }
    }
}
