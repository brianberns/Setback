# Counterfactual Regret Minimization for Setback

This project uses [Counterfactual Regret Minimization](https://github.com/brianberns/Cfrm) to solve the game of [Setback](https://en.wikipedia.org/wiki/Pitch_%28card_game%29#Auction_Pitch) (aka Auction Pitch).

## Approach
Setback's move tree is not as large as that of some other card games (e.g. Bridge, Hearts), but it is still too large for vanilla CFR on commodity hardware. The approach used here is to create a simpler abstract game that is essentially identical to Setback, but whose move tree is small enough for vanilla CFR. Defining such an abstraction is much more of an art than a science, relying heavily on personal intuition for what information is most salient when playing Setback.

## Information set keys
Keys are only created for infosets with more than one legal action. Each key is a string with the layouts described below.

### Auction key
#### Baseline
These keys are used when training the baseline (score-insensitive) model.
| Position | Name | Possible Values | Description |
|--|--|--|--|
| 0 | High bid | `0` (pass)<br/>`2` (two bid)<br/>`3` (three bid)<br/>`D` (dealer-overridable four bid) | High bid so far. |
| 1-6 | Ranks #1 | 6 rank characters, or `.` placeholders. E.g. `Jxx2..` | Ranks present in hand's strongest suit |
| 7-12 | Ranks #2 | Same as above | Ranks present in hand's second-strongest suit, if any, or `.` placeholders |

Suit strength is determined as follows:

* Convert the rank of each card in the suit to an integer by subtracting one from its face value. (E.g. Two → 1, Three → 2, ..., Ten → 9, Jack → 10, ..., Ace → 13).
* Sum the values.

The second-strongest suit is included iff the difference between its strength and the strength of the strongest suit is less than 2.

#### Bootstrap
These keys are used when bootstrapping a score-sensitive model from a baseline model. Unlike the baseline keys, these keys are formatted using `/` characters, rather than by absolute position within the string, as follows:
```
Score-need/High-bid/High-bidder/Num-bids/Hand
```

Where:
* `Score-need`: Representation of the current score, consisting of two characters: `Them-need` and `Us-need`.
    * `Them-need`: The number of points the other team needs to win, or `x` if the other team needs more than four points (i.e. can't win on this deal).
    * `Us-need`: If the other team is ahead and needs four points or less then `!`, else `.`.
* `High-bid`: The current high bid as an integer: `0`, `2`, `3`, or `4`.
* `High-bidder`: Index of the current high bidder, relative to the dealer: `0`, `1`, `2`, `3`, or `-1` if there is no current high bidder.
* `Num-bids`: The number of bids made so far in this auction (`0` - `3`).
* `Hand`: The bidder's hand, as expressed in positions 1-12 of the baseline auction key.

### Playout key
#### Establish trump
These keys are used by the auction winner to establish trump with the first card played.
| Position | Name | Possible Values | Description |
|--|--|--|--|
| 0 | Header | `E` | Establish trump. |
| 1-13 | Auction key | | Corresponding auction key. |

#### Normal playout
A normal playout key is a string of up to 24 characters, laid out as follows:
| Position | Name | Possible Values | Description |
|--|--|--|--|
| 0 | High established | `H` (true)<br/>`.` (false) | Indicates whether the auction winner led a high card on the first trick. |
| 1 | Low taken | `2`<br/>`3`<br/>`4`<br/>`5`<br/>`x` (rank > 5, or current hand holds a lower trump) | Rank of the lowest trump card taken so far. |
| 2 | Jack taken | `J` (true)<br/>`.` (false) | Indicates whether the Jack of trump has been taken. |
| 3 | Game delta | `+` (current team is ahead or tied)<br/>`-` (current team is behind) | Indicates how many Game points the current team has taken, relative to the other team. |
| 4 | Trump voids | `0`-`F` | A hex digit that indicates which of the other players are known to be void in trump (because they failed follow suit on a trump lead). |
| 5 | Rank #1 | `2`-`5` (possible low trump)<br/>`T`-`A` (has Game value)<br/>`x` (other ranks)<br/>`.` (not played yet) | Rank of first card played on current trick. |
| 6 | Suit #1 | `t` (trump)<br/>`x` (non-trump)<br/>`.` (not played yet) | Suit of first card played on current trick. |
| 7 | Rank #2 | Same as above. | Rank of second card played on current trick. |
| 8 | Suit #2 | Same as above. | Suit of second card played on current trick. |
| 9 | Rank #3 | Same as above. | Rank of third card played on current trick. |
| 10 | Suit #3 | Same as above. | Suit of third card played on current trick. |
| 11 | Trick winner | `0`-`3`<br/>`.` (none yet) | 0-based index of card play that is winning the current trick. |
| 12-13 | Legal action #1 | `Tr` (play trump of rank `r`)<br/>`S.` (lead a strong non-trump)<br/>`W.` (lead a weak non-trump)<br/>`Wx` (play a winning trump)<br/>`Lx`(play a losing trump)<br/>`wg` (follow suit with card worth `g` Game points to win the trick)<br/>`lg` (follow suit with card worth `g` Game points to lose the trick)<br/>`T.` (contribute a Ten)<br/>`G.` (contribute a non-Ten card worth Game points)<br/>`D.` (duck - play a weak card) | Legal action available in this situation. |
| 14-15 | Legal action #2 | Same as above, or `..` placeholder. |
| 16-17 | Legal action #3 | Same as above, or `..` placeholder.. |
| 18-19 | Legal action #4 | Same as above, or `..` placeholder.. |
| 20-21 | Legal action #5 | Same as above, or `..` placeholder.. |
| 22-23 | Legal action #6 | Same as above, or `..` placeholder.. |

Trailing dots (`.`) are trimmed from the baseline keys to save space. (There are a lot of them, especially the playout keys.)

## Usage

1. Run the `TrainBaseline` project to generate a baseline strategy profile (`Baseline.strategy`) that is optimized to play a hand of Setback without regard to the score of the game. *Warning*: This requires a machine with at least 16GB of RAM, and will take several weeks/months. I recommend at least 10 million CFR iterations.
2. Copy `Baseline.strategy` into the `TrainBootstrap` project and then run the project to bootstrap a strategy profile (`Bootstrap.strategy`) that is optimized for score-aware bidding (e.g. by bidding more aggressively if the opposing team is close to winning the game). This requires less time and RAM. I recommend at least 20 million CFR iterations.
3. Copy `Baseline.strategy` and `Bootstrap.strategy` into the `LoadDatabase` project, and then run the project to create a SQLite database that can be used to play Setback (via the `DatabasePlayer` module).
4. Copy the SQLite database `Setback.db` into the `Setback.Cfrm.PlayGui` project, and then run the project to play against the trained model (Windows only).
<!--stackedit_data:
eyJoaXN0b3J5IjpbNTUzNTI1MzM3LDc4OTM5MTMwNSw3MDI1Nz
E1ODksMTU4NjE1NzUzNCwtNjg2MjQwNDY4LDEzMjE2NTIyMSwy
MDc1NTg3MDk3LC04MTIwMzkyNDBdfQ==
-->