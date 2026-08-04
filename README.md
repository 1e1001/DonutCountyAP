# DonutCountyAP
Archipelago implementation for Donut County (2018)

[Setup guide](./donutcounty/docs/setup_en.md)


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
		- types of catapultable items
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
		- type: every type of object gets a location and item
		- all: every object gets a location and item
	- every eatable object is a check
		- infinitely-repeatable objects (water balloon, flashlight battery) only item the first time
	- game applies size bonus from every check in the current segment(what's the proper name for that)
		- they're called cameras in-game, that's a stupid name
	- game ignores special effects (e.g. transitions) from items that don't have their check unlocked
	- objects that do nothing (how many are there?) have no item
	- "how many are there?" is a good question in general - do i need to catalogue every object?

game options:
- goal options:
	- goal_area
		- bossfight: Beat the bossfight level to unlock Aftermath
		- aftermath: Fragments directly unlock Aftermath
	- total_fragments (0-??): How many fragments to add to the item pool. There may be fewer than this depending on the number of free locations.
	- framents_required_percent (0-100): Percentage of total fragments that need to be collected to unlock the goal area.
- items:
	- water (bool): Require an item to be able to fill your hole with water or soup (1 item)
	- fire (bool): Require an item to be able to fill your hole with fire, or to have fire ignite things in the world (1 item)
	- snake (bool): Require an item to be able to fill your hole with a snake (1 item)
	- light (bool): Require an item to be able to fill your hole with light (1 item)
	- catapult:
		- off: Catapult is always available
		- global: Require an item to use the catapult (1 item)
		- indiviudal: Each type of launchable object gets its own item (?? items)
	- trashsanity_items: Multiworld items grant object's hole size bonus and enable the object's effects
		- off: Regular game progression
		- type: Collect an item for every object type (?? items)
		- all: Collect an item for every object (?? items)
- locations:
	- level_completions (bool): Location for completing each level (21 locations)
	- level_segments (bool): Location for completing segments of levels (63 locations)
	- achievements (bool): Location for each non-postgame achievement (16 locations)
	- buy_catapult (bool): Location when purchasing the catapult (1 location)
	- trashsanity_locations: If set lower than trashsanity_items, will inherit its value.
		- off: You only need the mechanics to get through the game
		- type: Every type of trash grants a location (?? locations)
		- all: Every trash object grants a location (?? locations)
	- snake_danger (bool): Ranger Station snake danger is randomized (4 locations, 4 items)
	- salt_and_pepper (bool): Cat Soup shakers will give locations, progressive salt & paper unlock progression (5 locations, 5 items)
	- hack_protocol (bool): Raccoon HQ's entrance is randomized (1 location, 1 check)
- trash:
	- filler_weights:
		- true_filler: BK boes one (1) backflip
		- concrete_trap: Disable your hole for a short period of time

GameState storage:
- public fields for game settings
- or! a map<string, int> + map<string, bool> - for string-based item ids
- int[] of items + bool[] of locations
	- for "trigger-based" usage, not polling
	- how to deal with items/locations that don't exist due to settings
		- just check the settings
	- how to do realtime updates
		- have a randomizer manager that tracks every object
		- every object has a RandomizerFallState that tracks the extra info i need
		- patch hole to respect that behavior
- constants for item ids

extra qol:
- auto text skip
	- cutscene levels aren't included in the rando, but there's mid-level dialog
- achievement assistance
	- give secret code infinite debounce (the code never doubles an input)
	- Quack Enthusiast should take fewer quacks (10)
	- Egg Breaker should take fewer eggs (12)
- abilities tracker
	- maybe this can be the missing menu item?
- trashsanity tracker
	- trashopedia shows collection status next to item
	- optional in-level indicator (some billboard above item?)
- patch clickable transitions?
	- raccoon hq entry could be a fun glitch logic.
	- tk's door should just kick you to the menu
- i wonder if i can inject tags into the language data (HoL reference :3)

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
		- `trashopedia_index` menu position, save this in our own settings
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
	- ap layout: Levels, Connect, Options, Exit / Trashopedia, Abilities, Credits
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
	- i need a table for doing item id -> gameobject path(?), so might as well store some in-mod logic there (e.g. freeze until camera)



list of everything:
    0: Mira's House
		--: texting (level start)
		0: logo zoom in
		1: finish
    1: (BK texting cutscene)
		no level check
    2: Potter's Rock
		0: intro
		1: front yard
		2: side (potter)
		3: rear (pots)
		4: launch air balloon
		5: finish
    3: Ranger Station
		0: snakes
		1: snake hole
		2: snake danger
		3: finish
    4: Riverbed
		0: small dog
		1: finish
		2: unused
    5: Campground
		0: collect campfire
		1: popcorn
		2: finish
    6: Hopper Springs
		0: collect carrots
		1: bunnies 1
		2: bunnies 2
		3: bunnies 3
		4: finish
    7: Joshua Tree
		0: arson
		1: finish
    8: Beach Lot C
		0: first firework
		1: collect bird
		2: three firewords
		3: finish
    9: Gecko Park
		--: texting cutscene (level start)
		0: geckos
		1: guy
		2: finish
    10: Chicken Barn
		0: exterior
		-: toilet (StoreHelper.onCompleteStore)
		1: catapult tutorial
		2: chicken toss
		3: egg toss
		4: finish
    11: Honey Nut Forest
		0: get frog
		1: frog tutorial
		2: finish
    12: Cat Soup
		0: outside
		1: cooking
		2: dining
		3: houses
		4: finish
    13: Donut Shop
		--: quadcopter cutscene
		0: zoomed in
		1: finish
    14: Abandoned House
		0: dark
		1: light
		2: finish
    15: Raccoon Lagoon
		0: first pump
		1: log flume
		2: collect holder
		3: wheel
		4: finish
    16: The 405
		0: small car
		1: truck repair
		2: three raccoons (QuadCopterBigBoy.Entrance_Enter)
		3: polar bear
		4: finish
    17: Above Donut County
		no level check
    18: Raccoon HQ (Exterior)
		--: texting cutscene
		0: collecting
		1: ignore
		2: eat stick
		3: hacking
    19: (hq entrance)
		no level check (part of previous level)
		0: free roam
    20: Biology Lab
		0: collect snake
		1: using snake
		2: using frog
		3: leaving
    21: (hallway to anthropology)
		no level check (part of previous level)
		0: free roam
    22: Anthropology Lab
		0: collect bin
		1: first firework
		2: ignore?
		-: finish
    23: (hallway to trash king)
		no level check (part of previous level)
		0: free roam
    24: Trash King's Office (TKOfficeManager)
		2: donuts 1
		5: donuts 2
		8: donuts 3
		9: trap
    25: Boss Fight
		0: fight
		1: ignore
		2: ignore (plug in scene)
		3: explode boss
    26: Catapult
		no level check
    27: Aftermath
		no level check
    28: Game Over
		no level check (achievement)