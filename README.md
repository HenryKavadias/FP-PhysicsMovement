# FP-PhysicsMovement

## Overview
Template project for a First person physics based movement control system in the Unity game engine.

This uses code derived from tutorials by the YouTube channel Dave / GameDevelopment (https://www.youtube.com/@davegamedevelopment). 

I recommend looking at there works and tutorials to learn how all these systems work. I've used the movement systems tied to this playlist for this project (not including the third person movement) (https://www.youtube.com/playlist?list=PLK_5K9cAPN0fB6JD7gKE3aKSKHs7Hy0GW)

Here is the list of systems I've used for this project:
- First Person physics movement, with Slope movement, multi jumping, sprinting, and crouching
- Sliding movement
- Wall running, jumping, and climbing
- Dash ability
- Multi-Hook ability (grapple and rope swing with 1 to the N number of grappling hooks)

Ledge climbing is included but I don't recommend using this one, it's still buggy. (Possible to get stuck in the Unlimited movement state)

My goal was to take the movement systems outlined in his tutorials and do the following: 
- Integrate them into a SINGLE first person player character.
- Use the new input system.
- Have ALL the movement systems working together.
- Allow the developer to control which movement systems are active (Input Handler script)
- De-couple input reading and handling from the player character (have them attached to the camera object, which isn't a child object of the player)
- Make improvements and extra features (e.g. multi-jump)

This character movement system is intended to be used as a template and modified for other projects.

## Default Control Scheme (Keyboard and mouse only)

- Movement: WASD
- Jump: Space bar
- Sprint: X (can be toggle)
- Crouch: C (can be toggle)
- Slide: Left-CTRL
- Dash: Left-Shift
- Grapple && Rope Swing: Left-Mouse button (hold down Left-Alt for grapple when Rope swing is enabled as well)

## Details For Movement Abilities

Multi-Jumping:
The player can jump as many times as their jump limit. However if they run of a ledge with out jumping the first jump will count as 2, and if their jump limit is 1 or lower, then they won't be able to jump (first jump is biased to be a ground jump)

Dash:
Player can dash in any direction. Options and limitations to it can be enabled or disabled (e.g. can only dash again after player is grounded, which is triggered when a player stands on an object that is on the "Ground" layer)

Sliding:
Player can do a ground slide, similar to that of the dash, but they must be grounded and only in the direction they are facing. They can slider for a limited duration, but if the player is on a slope, they can sliding down it without duration limitations until they reach the end of it.

Wall Running, Jumping, Climbing:
Players can run, jump and climb across walls that are on the Layer "Wall". There options for vertical / diagonal wall running (Left-Shift for ascend, Left-CTRL for Descend) and disabling gravity while wall running. There is a limitation where the player can't wall run on the same wall forever. Once they disengage from a wall via timer or jumping off it, they can only run on that wall again if they then run on a different wall or ground themselves.

Multi-Hook:
This system allows for any number of hooks to be used for grappling and rope swinging movement. When the input triggers it, all hooks that are able to connect with any valid object will fire and connect they player to that point. With rope swinging, the option is available for the player to have directional movement while connected. This also includes extending the ropes when moving backwards, and reducing rope length and applying directional force to the player when pressing the jump button. There is also a basic rope grapple animation built in, and the aim of each hook is determined by the rotation of it's respective point aimer (empty object attached to the player)
