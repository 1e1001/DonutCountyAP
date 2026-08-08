# DonutCountyAP
Archipelago implementation for Donut County (2018)

[Setup guide](./donutcounty/docs/setup_en.md)


## design notes / todo:

mvp todo:
- item gates (snake danger, salt & pepper, hack protocol)
- apworld groups

extra qol:
- auto text skip
	- hold down key for 0.5s
	- allow pressing chat buttons with space
- level select changes
	- no selecting useless levels (above donut & catapult)
	- ui for locked level
	- allow clicking on level circles
- level select (locations) tracker
	- to the right of the preview image, add a grid of dots
		- rows are regions of the level
		- first dot is segment completion, more dots for each notable location, then a numeric counter for trashsanity
- abilities tracker
	- in main menu?
	- or in the pause stats again
- trashsanity tracker
	- trashopedia shows collection status next to item
	- optional in-level indicator (some billboard above item?)

## Developer guide

- `logic.xml` is a spreadsheet containing all the item/location info
	- if you change it, run `autologic.fsx` and commit the changes
- To get working mod builds:
	- add a modded copy of the game as `lib/Donut County/`
	- add a compiled copy of [`c-wspp-websocket-sharp`](https://github.com/black-sliver/c-wspp-websocket-sharp/) to `lib/wspp/`
- To develop APWorld, hardlink (junction) the `donutcounty/` folder into your from-source Archipelago's `worlds/` folder