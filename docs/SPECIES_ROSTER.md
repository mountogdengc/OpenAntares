# Species Roster

**Status: Accepted creative direction. No implementation is claimed.**

This roster names the four peoples who enter the Antares Verge, and the limit that makes each of them play differently. The [charter](CHARTER.md) remains authoritative for scope. The [design guide](DESIGN_GUIDE.md) remains the guidance for systems. The [economy specification](FIRST_PLAYABLE_ECONOMY.md) remains the rule for the one-colony loop. Nothing in this document changes those formulas, the `verdant_world` starting fixture, or the current milestone.

Ages in the sheets are there so a reader can picture a life. They are not turn counts, and they are not modifiers.

## Decisions

| Question | Settled choice | Notes |
| --- | --- | --- |
| What this pass produces | Four prose species sheets. | Content files, colony-loop modifiers, and a custom creator wait for their own specifications. |
| How alien the bodies are | Three peoples you could sit with, and one life that is a single body. | The singular life is the Wenth. It can grow a limb that sits and speaks. The colony is still one body, and the limb is not a person. |
| How many | Four. | A larger pick list was set aside so each entry can keep a different job. |
| Kind of difference | One structural limit each. The limit closes an ordinary plan and opens another. | A species whose identity is a percentage bonus does not satisfy this roster. |
| Narrow habitat | Kept. The Sulan home colony is comfortable. Their real choice arrives with other worlds. | That limit is quiet during the one-colony game, and the sheet says so. |
| Culture | Each sheet records the culture that reaches the Verge. | Biology stays separable. A later custom setup may pair a body with another culture. This roster does not define that setup. |

## The Verge

Navigators call the region the Antares Verge, after the red star they use as a bearing. It is a sparse volume. The Peldra, the Harl, the Sulan, and the Wenth each developed starflight on their own world. They are only now finding one another's tracks. These four are the playable roster. The document does not decide what else lives in the Verge.

## What a sheet records

Each sheet records:

- a string ID, a name, and a one-line hook
- the body
- what counts as a home
- the culture that reaches the Verge
- the plan that fails
- the choice that becomes attractive
- the colony-loop lever that can carry the limit later
- the part that waits for expansion, research, or contact

A workforce unit in the current economy is one assignment of labor to support, production, or research. For the Peldra, the Harl, and the Sulan, that unit is a person. For the Wenth, that unit is a lobe of the one body. This roster does not add a second population type for children, dependents, or speaking-limbs.

## How the four differ

| Species | ID | Closed plan | Attractive choice | Colony-loop lever | Waits for |
| --- | --- | --- | --- | --- | --- |
| Peldra | `peldra` | Split a small population evenly across support, industry, and research. | Feed them first, then spend scarce adults on the work that matters. | High support need per person, high output from each person who is fed, slow growth. | Founding a colony, or replacing the dead, by moving or raising adults who were already the labor. |
| Harl | `harl` | Keep a food surplus and boom to the cap. | Cover support without a surplus, and spend people on industry or research while room remains. | Surplus growth that reaches the starting capacity during the opening, and discarded growth once it does. Exact support still allows ordinary base growth. | Replacing a lost archive, which stays harder than replacing a lost population. |
| Sulan | `sulan` | Settle the next pleasant-looking world as if it were home. | Deepen the home shelf. Make the water right before sending people to live on it. | None on the starting colony. Support, growth, and output follow the ordinary rules at home. | World differences of water, shelf, and salinity, and a project that can make a sea. |
| Wenth | `wenth` | Grow spare people and ship them as settlers. | Stay inside the spread the body can coordinate. Found by fission. | Lobes are the workforce, under a low ceiling. Each lobe is potent. | The fission itself, which leaves parent and daughter smaller until they regrow. |

## Peldra

One fed adult does the work of a crew, and eats like one.

They are bipedal and dense: tall enough to sit at another species' table, heavy enough that the chair is built for them. The hide is thick. The hands are broad. A childhood lasts longer than some species' working lives, and a prime lasts well over a century. They eat through the working day. A cold kitchen stops the estate as surely as a broken forge.

Home is a heavy, fertile world with thick air and a long growing season. Calories and atmosphere have to be in hand before anyone can be spared for a workshop.

The Peldra who reach the Verge live as estates. An estate is a kitchen, an air plant, a workshop, and the adults who keep them. House-masters meet as a council when a choice crosses estate lines. A craft is learned by staying with one master for what a shorter-lived people would call a generation. The person who starts a harbor is often the person who finishes it.

An even split of a small population across support, industry, and research leaves the estate underfed, and the work stops. Feed them first. Once support holds, put those scarce adults on the job that matters.

A later economy change may express this as a high support need per person, high output from each person who is fed, and slow growth. The support obligation is the identity. The output follows from the body being a large share of the colony's labor. Founding another colony, and replacing the dead, both wait on moving or raising adults who were already the labor.

## Harl

A colony plan outlives the people who began it.

They are slight, warm, and quick in the hands and the voice. A Harl is adult within a year and old past twenty. They sleep little. A room of them sounds crowded, because an answer starts before the previous sentence has finished.

Home is mild and food-rich. A surplus becomes children. The world fills.

The Harl who reach the Verge are a league of schools. A school trains a craft, keeps that craft's archive, and buries its own graduates. Shipyards and laboratories belong to schools. A method that lives only in one pair of hands is buried with them, so they write decisions down as a habit.

A food surplus runs the colony up to its population cap, and further growth is thrown away. The people who began the long work are already gone. Cover support without a surplus, and spend those short lives on industry or research while the world still has room.

Under the current economy, a colony with no support deficit still gains the ordinary base growth. Choosing against a surplus is the choice this roster means. Freezing population outright would be a new rule, and this roster does not add one. A later economy change may make Harl surplus growth reach the starting capacity during the opening, with discarded growth visible in the forecast. Replacing a lost population stays easy later. A burned archive is a later loss, and it costs more than the population did.

## Sulan

A green world is a home only where the water is right.

They are upright and long-limbed, with skin that has to stay damp and a lung helped by a fold that draws oxygen from wet air. They walk, sit, and use tools. A dry room will do for a meeting. A dry continent is a visit.

Home is shallow, mineral-heavy water on a broad shelf, with tides that refresh it. Their cities are harbors grown back into the marsh. Open prairie feeds the eye and fails the Sulan.

The Sulan who reach the Verge are harbor houses under water law: who may cut a channel, who may change a salinity, whose silt this is. Pilots and terrace-builders hold the practical authority. Children learn channels the way other peoples learn streets.

The plan that fails is settling the next pleasant-looking world as if it were home. Deepen the home shelf. When expansion exists, make the water right before sending people to live in it.

On the starting colony, which is home, support, growth, and output follow the ordinary rules. The Sulan gain a colony-loop penalty nowhere in this roster. Their choice waits until worlds can differ by water, shelf, and salinity, and until a project can make a sea. The current `verdant_world` type does not record those differences.

## Wenth

A new world is a wound that becomes a daughter.

A Wenth is one organism. Filaments run through warm mud. Rafts of tissue spread on the shallow water. It grows the organ the work requires: a feeding mat, a making-limb, a knot that holds a memory, or a temporary body that can sit and speak. That speaking body is a limb. It has no childhood and keeps no office. When the talk is finished, the Wenth takes it back. Signals travel through the tissue. Past a certain spread, the body ceases to be one mind.

Home is a warm wetland deep enough to anchor filament and shallow enough for the rafts to breathe. The colony is that body and the works it has extruded.

Temperament is what the body has practiced. The Wenth entering the Verge learned to fire a sealed lobe inside a ceramic seed, because it wanted to know what lay past the marsh. Another Wenth on another world may care about nothing beyond its own water.

Growing a spare population and shipping settlers fails, because a Wenth has no spare people. Anything that leaves is cut out of the body. Stay inside the spread the body can coordinate, and spend the lobes on the work. A second colony is a fission. Parent and daughter are both smaller until they regrow.

On the colony screen the lobes are the workforce, assigned to support, production, or research, under a low ceiling. The ceiling stands for the spread at which the body stops being one mind. Each lobe is potent, so a small Wenth still builds and studies. The fission itself waits until a second colony can be founded. Diplomacy, when it exists, meets the speaking-limb and is speaking to the whole marsh.

## What this roster leaves unset

This document sets no numeric modifiers, no content schema, and no portraits. It does not define planet-trait fields, a fission command, ship crews, archives as game objects, or a custom civilization builder. It does not add species to the first playable economy.

A later implementation has to amend the economy specification before any of these levers exist in rules. Explanations of support, growth, capacity, and output have to name the species when the species is the source. Saves and content validation follow the project rules already set for other content: string IDs, load-time errors that name the file and the entry, and whole-number calculations.

## Checks against this roster

A later species implementation matches this roster when all of these hold:

- The four IDs are `peldra`, `harl`, `sulan`, and `wenth`.
- The failing plans stay distinct: an underfed even split, a boom into the cap, settling the next green world, and shipping spare settlers.
- The Peldra identity remains the support obligation of a scarce adult. A flat bonus with an ordinary support need does not pass. On the home world, the economy spec's starting assignment of two support, two production, and two research leaves a Peldra colony short of support.
- The Harl identity remains surplus growth against the cap. A Harl colony that keeps a support surplus reaches the economy spec's starting capacity of 10 while that capacity is still the starting value, and the forecast shows discarded growth. A Harl colony that covers support without a surplus stays under that capacity for longer. Faster growth that never meets this test does not pass.
- The Sulan starting colony follows the ordinary support, growth, and output rules.
- A Wenth colony is one body. Its workforce units are lobes. Its starting ceiling is lower than the economy spec's starting capacity of 10. Founding another colony, once founding exists, reduces the parent.
- Peldra scarcity is the cost of keeping a person. Wenth scarcity is the spread of one mind. Giving those two the same support need, the same ceiling, and the same growth does not pass.
- The speaking-limb is not a person, a leader, or a stored population unit.
- Default cultures can be named from these sheets, and the biology can be referred to without requiring that culture.
