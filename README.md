# Counterfactual Regret Minimization for Setback
This repository uses [Counterfactual Regret Minimization](https://github.com/brianberns/Cfrm) to solve the game of [Setback](https://en.wikipedia.org/wiki/Pitch_%28card_game%29#Auction_Pitch) (aka Auction Pitch).
# Usage
1. Run `TrainBaseline` to generate a baseline strategy profile (`Baseline.strategy`) that is optimized to play a hand of Setback without regard to the score of the game. *Warning*: This requires a machine with at least 16GB of RAM, and will take several weeks/months. I recommend at least 10 million CFR iterations.
2. Copy `Baseline.strategy` into the `TrainBootstrap` and r
<!--stackedit_data:
eyJoaXN0b3J5IjpbLTE3MDM0NzYwOTFdfQ==
-->