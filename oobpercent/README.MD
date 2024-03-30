A better readme will be made later, sorry
中文 README 之后也会写的 ;-;

# OutofBound%

This is a Human: Fall Flat plugin which introduces a new objective: OutofBound% (oob% in short),
where players need to reach beyond the boundary of the level to finish it.

## How does the plugin work
For a more accurate and more detailed explanation, dive into the code (I've added comments on most of the important codes)

### 1. Validate the level and finds the bound
In order to play oob%, a level must have a boundary.  

Boundaries are defined as the edge of the FallTrigger (a big trigger zone which respawns players whenever they fall into it), 
more accurately speaking, the largest FallTrigger that is below the spawn.

The plugin performs a logic to test if the level is valid: 

1. It finds all falltriggers from the value
2. the ones which does not have a BoxCollider are skipped
3. the ones with their top face above (or equal to) the spawnpoint height are skipped
4. the ones where the spawnpoint is outside of its horizontal range are skipped

If there is no valid falltrigger selected, the level would be marked as invalid for oob%

5. From the valid falltriggers found above, the plugin finds the one with the largest horizontal scale
6. It calculates the range of it (-X -Z +X +Z coordinates which defines the range)

### 2. Moddifying the pass (finish level) logic
In oob%, the original passtriggers (pass zones) of the level are removed. 

The plugin:  

1. looks for passtriggers (passzones) from the level
2. it removes every passtriggers
3. if there is no passtrigger in the beginning, you'd not be able to pass the level even with oob
   (it will be explained more detailed in the next section)

### 3. Checking for OOB and triggering OOB event
In order to finish the level in oob%, you would need to find a way to get out of bound.

On every time interval (like when game logics are ticked):

1. the positions for every players are updated
2. the plugin checks if any of their possition is out of the range

When an out of bound is found: 

- Normally the plugin emulates the event of entering the passtrigger and fall into the falltrigger right after. This ensured that it is as close to vanilla as possible

- If the level contains no passtrigger in the beginning, players would not be able to finish the level on oob, but they will still be respawed.

## Toggle OOB without restarting the game
You can toggle oob via the in-game console (open console with `~` or `f1` on your keyboard)

Command syntax: `oobmode(oob) <on(1), off(0), toggle(t), query(q)>`  
(`< ... >` represents the command arguments, like if you input `oobmode on` or simply `oob 1` it turns oob% on)

Accepted arguments: 
- `on` or `1`: Turn it on
- `off` or `0`: Turn it off
- `toggle` or `t`: Toggles it between on and off
- `query` or `q`: output the current on/off state with out changing it
- any other arguments or no arguments at all will simply do nothing

Whenever the plugin is started, oob% will turn on by default. There is no external config currently and you need to 
manually turn it off.

## Invalid levels and cheating
Whenever the oob% on/off state is changed, the run will be invalid and you'd need to restart it. 
(with plcc timer installed you'll be able to see if the current run is invalid or not).

> if it is already on and you run `oob on` for example, the run will not be invalid

If the level doesn't satisfy the requirements of oob%, a warning will be shown on gui (with propper plcc timer integration of course)