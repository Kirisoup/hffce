> ## Important Terms: 
> - "FT" is the short hand for FallTrigger
> - "BoundBox" refers to the FallTrigger which should represent the boundary of the level
> - "the plugin shits itself  :(" refers to when the plugin comes up with a boundary that doesn't make any sense

1. FTs without a BoxCollider are not considered.
   - Therefore if the actual BoundBox is lets say in shape of a cylinder, it will be ignored. 
     If in this case there happeded to be another boxy FT under the spawn, the plugin will instead select it as the BoundBox, therefore it shits itself :(
     Else the level will be intepreted as invalid.
   - If there are any cool maps that uses like a cylinder FT, and ppl want to play oob on it, please issue me and I might implement round boundary logic
     or open a pull request :3 

2. if the spawnpoint for the level is for whatever reason under the actual BoundBox -- or outside of it, it shits itself :(
   - like the spawn is at a small intro section that is seperate from the rest of the map

3. If there are *rOtaTeD* FallTriggers, the plugin will definately shit itself :(
   - Applies for other transformations if there are any
   - Because X Y Z are not cardinal anymore, the BoundBox that the plugin come up with will be awkward
     Maybe there are ways to see if something is rotated, but unless this becomes a problem I guess I'm not fixing it :|

4. If the BoundBox is moving arround, the plugin will definately shit itself :(
   - Like generally what the fuck

5. If the author of the level is retarded and put toggeter multiple FallTriggers as the BoundBox, plugin will shit itself :(
   - and author should be arrested for their stupidity

6. If the author does not use the builtin FallTrigger object, instead some other random trigger zones, 
   to emulate the behaviour of a fall trigger, those will of course be ignored

6. If there are other monstrous edge cases that makes the plugin shit itself, please let me know because I am curious