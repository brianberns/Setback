# Counterfactual Regret Minimization for Setback
This repository uses [Counterfactual Regret Minimization](https://github.com/brianberns/Cfrm) to solve the game of [Setback](https://en.wikipedia.org/wiki/Pitch_%28card_game%29#Auction_Pitch) (aka Auction Pitch).
## Approach
Setback's move tree is not as large as that of some other card games (e.g. Bridge, Hearts), but it is still too large for vanilla CFR. The approach used here is to create a simpler, abstract game that is essentially identical to Setback, but whose move tree is small enough for vanilla CFR. This abstraction is much more of an art than a science, relying heavily on personal intuition for what information is most salient when playing Setback.
## Information set keys
Keys are only created for infosets with more than one legal action.
### Auction key
#### Baseline
| Position | Name | Possible Values | Description |
|--|--|--|--|
| 0 | High bid | `0` (pass)<br/>`2` (two bid)<br/>`3` (three bid)<br/>`D` (dealer-overridable four bid) | High bid so far. |
| 1-6 | Ranks #1 | 6 rank characters, or `.` placeholders. E.g. `Jxx2..` | Ranks present in hand's strongest suit |
| 7-12 | Ranks #2 |  | Ranks present in hand's second strongest suit, if any |

#### Bootstrap
### Playout key
#### Establish trump
This is a key used by the auction winner to establish trump with the first card played.
| Position | Name | Possible Values | Description |
|--|--|--|--|
| 0 | Header | `E` | Establish trump |
| 1-13 | Auction key | | Corresponding auction key (including possible high bid value of `4`) |
#### Normal playout
A normal playout key is a string of up to 24 characters, laid out as follows:
| Position | Name | Possible Values | Description |
|--|--|--|--|
| 0 | High established | `H` (true)<br/>`.` (false) | Indicates whether the auction winner led a high card on the first trick. |
| 1 | Low taken | `2`<br/>`3`<br/>`4`<br/>`5`<br/>`x` (rank > 5) | Rank of the lowest trump card taken so far. |
| 2 | Jack taken | `J` (true)<br/>`.` (false) | Indicates whether the Jack of trump has been taken. |
| 3 | Game delta | `+` (current team is ahead or tied)<br/>`-` (current team is behind) | Indicates how many Game points the current team has taken, relative to the other team. |
| 4 | Trump voids | `0`-`F` | A hex digit that indicates which of the other players are known to be void in trump (because they failed follow suit on a trump lead). |
| 5 | Rank #1 | `2`-`5` (possible low trump)<br/>`T`-`A` (has Game value)<br/>`x` (other ranks)<br/>`.` (not played yet) | Rank of first card played on current trick. |
| 6 | Suit #1 | `t` (trump)<br/>`x` (non-trump)<br/>`.` (not played yet) | Suit of first card played on current trick. |
| 7 | Rank #2 |  | Rank of second card played on current trick. |
| 8 | Suit #2 |  | Suit of second card played on current trick. |
| 9 | Rank #3 |  | Rank of third card played on current trick. |
| 10 | Suit #3 |  | Suit of third card played on current trick. |
| 11 | Trick winner | `0`-`3`<br/>`.` (none yet) | 0-based index of card play that is winning the current trick. |
| 12-13 | Legal action #1 | `Tr` (play trump of rank `r`)<br/>`S.` (lead a strong non-trump)<br/>`W.` (lead a weak non-trump)<br/>`Wx` (play a winning trump)<br/>`Lx`(play a losing trump)<br/>`wg` (follow suit with card worth `g` Game points to win the trick)<br/>`lg` (follow suit with card worth `g` Game points to lose the trick)<br/>`T.` (contribute a Ten)<br/>`G.` (contribute a non-Ten card worth Game points)<br/>`D.` (duck - play a weak card) | Legal action available in this situation. |
| 14-15 | Legal action #2 |  |
| 16-17 | Legal action #3 |  |
| 18-19 | Legal action #4 |  |
| 20-21 | Legal action #5 |  |
| 22-23 | Legal action #6 |  |

Trailing dots (`.`) are trimmed from the key to save space.

## Usage
1. Run the `TrainBaseline` project to generate a baseline strategy profile (`Baseline.strategy`) that is optimized to play a hand of Setback without regard to the score of the game. *Warning*: This requires a machine with at least 16GB of RAM, and will take several weeks/months. I recommend 10-20 million CFR iterations.
2. Copy `Baseline.strategy` into the `TrainBootstrap` project and then run the project to bootstrap a strategy profile (`Bootstrap.strategy`) that is optimized for score-aware bidding (e.g. by bidding more aggressively if the opposing team is close to winning the game). This requires less time and RAM. I recommend 20-30 million CFR iterations.
3. Copy `Baseline.strategy` and `Bootstrap.strategy` into the `LoadDatabase` project, and then run the project to create a SQLite database that can be used to play Setback (via the `DatabasePlayer` module).
<!--stackedit_data:
eyJoaXN0b3J5IjpbLTUyNjMxMTY3NywxNTg2MTU3NTM0LC02OD
YyNDA0NjgsMTMyMTY1MjIxLDIwNzU1ODcwOTcsLTgxMjAzOTI0
MF19
-->