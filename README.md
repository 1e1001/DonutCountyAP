# DonutCountyAP
Archipelago implementation for Donut County (2018)




## design notes / todo:

overall structure similar to celeste open world
- replace regular story progression with the level select screen
	- make completing a delivery go back to level select instead of continuing to the next
		- note that a few levels have weird end points (e.g. the labs actually end when entering the door)
- game progression sequence
	- play through all but 2 levels (goal level and credits) to collect checks
	- once a set percentage of goal unlocks are found, play the goal level to unlock the credits
		- alternatively, you can set the goal level to the credits, to skip this
	- open the credits level to goal
		- mod the credits to have some ap info (checks %, timer?, and credits)
- alternate goal: trashopedia
	- simply collect a large enough percentage of the object insanity items

locations / items:
- level completions (20/21 loc.)
	- Above Donut County, Catapult, and Aftermath don't exist
- segment completions (probably like 100 loc.)
- achievements (16-20 loc.)
	- four achievements are only available postgame (Redeemed, Nerd, Pilot, Escape)
- mechanics (2 items)
	- catapult
	- water
	- fire
	- flashlight
- goal level unlock (variable items)
	- limited to the number of free items, but preserves percentages
- traps / filler
	- bk barrel roll (makes the top-left icon spin i guess)
- in-level events? things that aren't just objects in hole
	- Ranger Station - snake danger (4 checks)
	- Chicken Barn - quadcopter purchase (1 check)
		- this just locks access to the second half of the level
	- Cat Soup - salt / pepper (5 checks)
		- need >= 1 salt/pepper to access the rest of the level, need all 5 for the achievement
	- Raccoon HQ - hacking (1 check)
- object insanity (many checks)
	- multiple levels of insanity
		- off: no locations, no items
		- key: only important objects get locations and matching items that enable their effects
		- all: every object gets a location and item
	- every eatable object is a check
		- infinitely-repeatable objects (water balloon, flashlight battery) only item the first time
	- game applies size bonus from every check in the current segment(what's the proper name for that)
		- they're called cameras in-game, that's a stupid name
	- game ignores special effects (e.g. transitions) from items that don't have their check unlocked
	- objects that do nothing (how many are there?) have no item
	- "how many are there?" is a good question in general - do i need to catalogue every object?

extra qol:
- auto text skip
	- cutscene levels aren't included in the rando, but there's mid-level dialog
- achievement assistance
	- give secret code infinite debounce (the code never doubles an input)
	- Quack Enthusiast should take fewer quacks (10)
	- Egg Breaker should take fewer eggs (12)

some implementation notes:
- `DataManager` needs to be replaced to not use the game's save data
	- preferences are unchanged, and i'll need an extra archipelago.pref for client settings
		- connection info
		- qol settings toggle
		- trashopedia index but better
	- check uses of `DataManager`, see if any need patching to allow for late initialization?
	- list of settings:
		- `current_delivery_index` specifies progress in-game, unused for AP (as we always load directly into levels)
		- `game_complete` 0 / 1 flag, obvious
		- `new_items_popup`, unused
		- `trashopedia_index` menu position, we need to modify the trashopedia so we can have our own index
		- `has_seen_gameover_cutscene` skips the gameover cutscene in situations? unused for us
		- ones in all caps are achievement progress
	- all other data needs to be derived from ap connection
		- received items
		- sent locations for tracker
		- settings (from slot data)
			- also include an apworld version for a mismatch warning
		- slot-local datastorage
			- current level / segment (for tracker)
- `RM` seems to be important, might be a useful place to mod things?
	- a lot of ui seems to never be unloaded, which is nice
- main menu design
	- current layout: Continue, New Game, Options, Exit / Trashopedia, Levels, Credits
	- ap layout: Levels, Connect, Options, Exit / Trashopedia, ??, Credits
		- despite being text from the original game, "Levels" should be untranslated?
	- use background scenes to indicate game state
		- void = disconnected
		- donut shop = ingame
		- beach = goaled
- connection menu design
	- just use a imgui menu for now, but try to make a stylized menu at some point
- trashopedia can be used as an in-game tracker
- for testing, i might need a custom menu to manually debug the received items / sent locations?
	- this needs to be in-game so i can revoke items (ap can't do this)
- try to design item info tables so that they can be shared with the apworld