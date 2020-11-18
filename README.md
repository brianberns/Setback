# Counterfactual Regret Minimization for Setback
This repository uses [Counterfactual Regret Minimization](https://github.com/brianberns/Cfrm) to solve the game of [Setback](https://en.wikipedia.org/wiki/Pitch_%28card_game%29#Auction_Pitch) (aka Auction Pitch).
## Approach
Setback's move tree is not as large as that of some other card games (e.g. Bridge, Hearts), but it is still too large for vanilla CFR. The approach used here is to create a simpler, abstract game that is essentially identical to Setback, but whose move tree is small enough for vanilla CFR. This abstraction is much more of an art than a science, relying heavily on personal intuition for what information is most salient when playing Setback.
### Playout key
A playout key is a string of up to 24 characters, laid out as follows:
| Position | Name | Possible Values | Description |
|--|--|--|--|
| 0 | High established | `H` (true)<br/>`.` (false) | Indicates whether the auction winner led a high card on the first trick. |
| 1 | Low taken | `2`<br/>`3`<br/>`4`<br/>`5`<br/>`x` (rank > 5) | Rank of the lowest trump card taken so far. |
| 2 | Jack taken | `J` (true)<br/>`.` (false) | Indicates whether the Jack of trump has been taken. |
| 3 | Game delta | `+` (current team is ahead or tied)<br/>`-` (current team is behind) | Indicates how many Game points the current team has taken, relative to the other team. |
| 4 | Trump voids | `0`-`7` | A hex value that indicates which of the other players are known to be void in trump (because they failed follow suit on a trump lead). |
| 5 | Rank #1 | `2`-`5` (possible low trump)<br/>`T`-`A` (has Game value)<br/>`x` (other ranks)<br/>`.` (not played yet) | Rank of first card played on current trick. |
| 6 | Suit #1 | `t` (trump)<br/>`x` (non-trump)<br/>`.` (not played yet) | Suit of first card played on current trick |

## Usage
1. Run the `TrainBaseline` project to generate a baseline strategy profile (`Baseline.strategy`) that is optimized to play a hand of Setback without regard to the score of the game. *Warning*: This requires a machine with at least 16GB of RAM, and will take several weeks/months. I recommend 10-20 million CFR iterations.
2. Copy `Baseline.strategy` into the `TrainBootstrap` project and then run the project to bootstrap a strategy profile (`Bootstrap.strategy`) that is optimized for score-aware bidding (e.g. by bidding more aggressively if the opposing team is close to winning the game). This requires less time and RAM. I recommend 20-30 million CFR iterations.
3. Copy `Baseline.strategy` and `Bootstrap.strategy` into the `LoadDatabase` project, and then run the project to create a SQLite database that can be used to play Setback (via the `DatabasePlayer` module).
<!--stackedit_data:
eyJoaXN0b3J5IjpbLTg3MjQyODU3NSwtODEyMDM5MjQwXX0=
-->