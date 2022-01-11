# Kinematic Body

A reference implementation of a KinematicBody intended for use with an upright
box collider that does not rotate.

This package should support any version of **Unity 2020.3** or newer.

> :warning: **WARNING**  
> This package is in an experimental state and has not been extensively
> reviewed or tested.

It is reflective of older games that utilize AABBs for their player volume, such
as the [Source Engine](https://developer.valvesoftware.com/wiki/Dimensions),
which adopts the following convention:

> The Player Hull must have a square footprint (like all the NPC Hulls), i.e. it
> can be as tall as you like, but width & depth must be the same. This is
> because the hull doesn't rotate with the players direction when detecting
> collision with the world.
>
> [Valve Developer Community, Player Entity](https://developer.valvesoftware.com/wiki/Player_Entity)

## Importing this Package

> **NOTE**  
> Support for OpenUPM will come as this library matures with instructor
> and student use.

### Git UPM Dependency

The KinematicBody package can be added as a Git dependency.

Git dependencies can be added the Unity Package Manager by providing the URL and ref to the UPM package:

```text
https://github.com/AIE-Seattle-Prog/KinematicBody.git#upm
```

You can also manually edit the `Packages/manifest.json` to include the following
in the `dependencies` array:

```text
    "com.aie-seattle-prog.kinematic-body": "https://github.com/AIE-Seattle-Prog/KinematicBody.git#upm"
```

For more information, review Unity's documentation on [Git dependencies](https://docs.unity3d.com/Manual/upm-git.html).

> **About Git Dependencies**  
> Git must be available on the command-line's PATH variable in order for Unity
> to resolve Git packages. If it is not available on the command-line, the
> package import will fail.

### Embedded Package

You can download a copy of the `upm` branch which contains the bare minimum
necessary files for a UPM package. The contents of this archive should be placed
into its own folder under the Packages folder in your Unity Project.

To verify, the package.json file from this package should be as follows:

```text
Assets/
ProjectSettings/
Packages/
    KinematicBody/
        package.json
        ...
```

## Quick Start

To review an expected setup for the character, review the sample scene under
**KinematicBody/Samples/SamplePlayerDemo.unity** in the package.

## API

The KinematicBody package provides two key classes and a sample class that
demonstrates how to use it.

![KinematicBody and KinematicPlayerMotor are the two framework classes consumed by SampleKinematicCharacter](.github/classDiagram.png)

The **KinematicBody** contains the bulk of the logic that defines the volume of
the kinematic body and any relevant APIs for querying with it.

It is referenced by the **KinematicPlayerMotor** which implements the
**IKinematicMotor** interface providing implementations for methods that will
be called by the body when it performs its "Move" update.

Users looking for a basic humanoid character can use this library as-is without
modification or much additional work aside from integrating it into their
player prefab and providing the appropriate inputs.

### Manipulating the Motor

The **KinematicPlayerMotor** is referenced by the **SampleKinematicCharacter**
which calls the `KinematicPlayerMotor.MoveInput` method to provide a world-space
input vector which tells the motor which way it should move.

Users should implement their own "Character" script that can provide inputs
with respect to other systems like their camera, current gameplay state, or
other game-specific considerations.

### KinematicBody Move Process

When the KinematicBody moves in Fixed Update, it follows this process:

![A diagram illustrating the steps taken to move the body w.r.t. the motor](.github/bodyMove.png)

All of the steps listed in a round white box are callbacks that are called by
the body at the appropriate stage in the process. Steps shown in gray and
rectangular boxes are performed internally by the body and may not necessarily
map 1:1 to a method.

### Collision Events

When a KinematicBody moves, it gathers collision and trigger events for
further processing.

User code can query this from the body by calling `KinematicBody.CollisionCount`
and `KinematicBody.TriggerCount` on an instance to determine how many collisions
or triggers it encountered. These can then be retrieved by calling
`KinematicBody.GetCollsion` and `KinematicBody.GetTrigger` accordingly.

> :warning: Events are not zeroed out between frames. You may encounter stale
> events if accessed too early or exceeding the count returned by `CollisionCount`/`TriggerCount`.

These are available after KinematicBody has run its FixedUpdate routine for that
frame.

> :warning: The max number of events per type is 32 (i.e. 32 collisions + 32 triggers)

#### Collision Messages

The KinematicBody can mimic "OnXXX" messages in Unity by enabling the "Send
Collision Events" boolean.

![KinematicBody with 'Send Collision Events' checked](.github/bodyInspector.png)

Once enabled, components can recieve collision messages by implementing methods
with the following signatures:

```csharp
    void OnKinematicCollision(KinematicBody.MoveCollision collision)
    {
        // react to collision
    }

    void OnKinematicTrigger(KinematicBody.MoveCollision trigger)
    {
        // react to trigger
    }
```

These are sent after `OnPostMove` has been called by the KinematicBody.

The events are received by both objects involved in the collision:

Body             | Other
-----------------|---------------------------------------
Body game object | Rigidbody game object
Body game object | Rigidbody game object comprising a compound collider/trigger
Body game object | Collider/trigger game object (if no Rigidbody)

This mimics the same rules that Unity applies when determine which game objects
receive collision messages with normal Rigidbody objects.

## License

This work is licensed under the MIT License. See [LICENSE.md](LICENSE.md) for details.

Copyright 2021 (c) Academy of Interactive Entertainment
