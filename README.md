# FP-PhysicsMovement
Template project for a First person physics based movement control system in the Unity game engine.

This uses code a originally sourced from YouTube tutorials from the channel Dave / GameDevelopment (https://www.youtube.com/@davegamedevelopment). 

I recommend looking at his works and tutorials to learn how all these systems work. I've used the movement systems tied to this playlist for this project (not including the third person movement) (https://www.youtube.com/playlist?list=PLK_5K9cAPN0fB6JD7gKE3aKSKHs7Hy0GW)

Here is the list of systems I've used for this project:
- First Person physics movement, with Slope movement, multi jumping, sprinting, and crouching
- Sliding movement
- Wall running, jumping, and climbing
- Ledge climbing (don't recommend using this one, it's still buggy. Possible to get stuck in the Unlimited movement state)
- Dash ability
- Grappling ability
- Mono-Swing ability
- Dual-Swing ability

My goal was to take the movement systems outlined in his tutorials and do the following: 
- Integrate them into a SINGLE first person player character.
- Use the new input system.
- Have (nearly) ALL the movement systems working together.
- Allow the developer to control which movement systems are active from a "player input controller" (currently just call player controller)
- De-couple the input reader and player controller from the player character (have them attached to the camera object, which isn't directly tied to the player)
- Improve code quality with the changes

This character movement system is intended to be used as a template and modified for other projects.
