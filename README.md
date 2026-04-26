# Deep CFR for Setback

This project uses [Deep Counterfactual Regret Minimization](https://arxiv.org/abs/1811.00164) (Deep CFR) to train an AI that plays the card game [Setback](https://en.wikipedia.org/wiki/Pitch_%28card_game%29#Auction_Pitch) (aka Auction Pitch) at a high level.

The approach used here is very similar to the one used previously to train a [Hearts AI](https://github.com/brianberns/Hearts). The main differences are:

* Because Setback is a smaller game than Hearts, we are able to use an encoding that exhibits "perfect recall" of past actions.
* Again because of the relative simplicity of Setback, we use a single model to address both individual deals within a game and strategy across deals within a game. In particular, good Setback strategy tends to bid more aggressively if the player's team is close to losing the game.

This is essentially version 2 of this repository. The previous version used ["vanilla" CFR](https://github.com/brianberns/CFR-Explained) on a simplified abstract version of Setback. This produced a very good player, but relied on expert developer Setback ability to construct the abstract game in a way that captured the essence of the game. Using Deep CFR, this is not necessary.
