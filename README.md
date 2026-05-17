# Deep CFR for Setback

## Overview

This project uses [Deep Counterfactual Regret Minimization](https://arxiv.org/abs/1811.00164) (Deep CFR) to train an AI that plays the card game [Setback](https://en.wikipedia.org/wiki/Pitch_%28card_game%29#Auction_Pitch) (aka Auction Pitch) at a high level.

The approach used here is very similar to the one I used previously to train a [Hearts AI](https://github.com/brianberns/Hearts). The main differences are:

* Because Setback is a smaller game than Hearts, we are able to use an encoding that exhibits "perfect recall" of past actions within the current deal.
* Again because of the relative simplicity of Setback, we use a single model to address both individual deals within a game and across deals within a game. In particular, good Setback strategy tends to bid more aggressively if the player's team is close to losing the game.

This is essentially version 2 of this repository. The [previous version](https://github.com/brianberns/Setback/tree/VanillaCfr) used ["vanilla" CFR](https://github.com/brianberns/CFR-Explained) on a simplified abstract version of Setback. This produced a very good player, but relied on expert developer Setback ability to construct the abstract game in a way that captured the essence of the game. Using Deep CFR, this is not necessary.

## Results

Iterations 1-5 were trained with a relatively small model:

* Hidden size: 1152 (same as input size)
* \# of hidden layers: 4

In iteration 6, this size model was able to match the strength of the previous vanilla CFR approach, winning ~50.1% of games against it in epoch 10. I was hoping for an even stronger result, however, so I increased the model size:

* Hidden size: 1200
* \# of hidden layers: 5

Retraining iteration 6 with this "large" size produced a model that won ~50.5% of games against the vanilla CFR champion in epoch 17. At this point, I discarded the vanilla CFR code entirely and chose this model as the provisional new champion.

I then increased the model size further to "x-large" and retrained generation 6 again:

* Hidden size: 1250
* \# of hidden layers: 6

This produced a model that beat the provisional large champion ~50.3% of the time in epoch 17. For good measure, I iterated one more time with this x-large model, producing another model that beat the provisional champion 50.3% of the time in epoch 13 of iteration 7. I played the two models head-to-head 260,000 times to verify.

### Win rate vs. Vanilla CFR champion

| Epoch | Iteration 1 | Iteration 2 | Iteration 3 | Iteration 4 | Iteration 5 | Iteration 6 | Iteration 6 large |
| ----- | ----------- | ----------- | ----------- | ----------- | ----------- | ----------- | ----------------- |
| 1     | 0.082       | 0.138       | 0.245       | 0.297       | 0.332       | 0.350       | 0.340             |
| 2     | 0.114       | 0.206       | 0.321       | 0.384       | 0.412       | 0.425       | 0.421             |
| 3     | 0.129       | 0.248       | 0.371       | 0.430       | 0.442       | 0.457       | 0.459             |
| 4     | 0.145       | 0.288       | 0.388       | 0.437       | 0.465       | 0.478       | 0.473             |
| 5     | 0.154       | 0.302       | 0.401       | 0.446       | 0.474       | 0.484       | 0.487             |
| 6     | 0.159       | 0.302       | 0.413       | 0.456       | 0.487       | 0.491       | 0.492             |
| 7     | 0.164       | 0.315       | 0.421       | 0.455       | 0.481       | 0.491       | 0.498             |
| 8     | 0.174       | 0.342       | 0.433       | 0.461       | 0.491       | 0.490       | 0.500             |
| 9     | 0.194       | 0.334       | 0.437       | 0.462       | 0.489       | 0.492       | 0.501             |
| 10    | 0.202       | 0.343       | 0.445       | 0.468       | 0.493       | 0.501       | 0.502             |
| 11    | 0.212       | 0.335       | 0.445       | 0.468       | 0.487       | 0.500       | 0.502             |
| 12    | 0.211       | 0.345       |             | 0.473       | 0.492       | 0.495       | 0.503             |
| 13    | 0.216       | 0.337       |             | 0.476       | 0.491       |             | 0.502             |
| 14    | 0.237       | 0.363       |             | 0.471       |             |             | 0.501             |
| 15    | 0.237       | 0.354       |             | 0.473       |             |             | 0.504             |
| 16    | 0.239       | 0.353       |             |             |             |             | 0.501             |
| 17    | 0.254       | 0.342       |             |             |             |             | 0.505             |
| 18    | 0.258       | 0.355       |             |             |             |             | 0.505             |
| 19    | 0.252       | 0.356       |             |             |             |             |                   |
| 20    | 0.244       | 0.356       |             |             |             |             |                   |
| 21    | 0.251       | 0.355       |             |             |             |             |                   |
| 22    | 0.258       | 0.351       |             |             |             |             |                   |
| 23    | 0.266       | 0.377       |             |             |             |             |                   |
| 24    | 0.266       | 0.359       |             |             |             |             |                   |
| 25    | 0.260       | 0.351       |             |             |             |             |                   |
| 26    | 0.258       | 0.367       |             |             |             |             |                   |
| 27    | 0.260       | 0.376       |             |             |             |             |                   |
| 28    | 0.259       | 0.360       |             |             |             |             |                   |
| 29    | 0.260       | 0.361       |             |             |             |             |                   |
| 30    | 0.256       | 0.370       |             |             |             |             |                   |
| 31    |             | 0.360       |             |             |             |             |                   |
| 32    |             | 0.357       |             |             |             |             |                   |
| 33    |             | 0.348       |             |             |             |             |                   |

### Win rate vs. provisional Deep CFR champion

| Epoch | Iteration 1 | Iteration 2 | Iteration 3 | Iteration 4 | Iteration 5 | Iteration 6 | Iteration 6 large | Iteration 6 x-large | Iteration 7 x-large |
| ----- | ----------- | ----------- | ----------- | ----------- | ----------- | ----------- | ----------------- | ------------------- | ------------------- |
| 1     | 0.055       | 0.108       | 0.221       | 0.266       | 0.311       | 0.336       | 0.318             | 0.289               | 0.305               |
| 2     | 0.078       | 0.177       | 0.291       | 0.360       | 0.396       | 0.410       | 0.405             | 0.397               | 0.409               |
| 3     | 0.100       | 0.217       | 0.347       | 0.405       | 0.429       | 0.441       | 0.446             | 0.432               | 0.455               |
| 4     | 0.109       | 0.253       | 0.372       | 0.423       | 0.459       | 0.464       | 0.467             | 0.458               | 0.472               |
| 5     | 0.111       | 0.269       | 0.385       | 0.431       | 0.469       | 0.474       | 0.477             | 0.473               | 0.485               |
| 6     | 0.120       | 0.287       | 0.399       | 0.442       | 0.468       | 0.485       | 0.483             | 0.489               | 0.491               |
| 7     | 0.127       | 0.304       | 0.406       | 0.453       | 0.472       | 0.486       | 0.487             | 0.491               | 0.493               |
| 8     | 0.137       | 0.308       | 0.410       | 0.456       | 0.477       | 0.489       | 0.493             | 0.486               | 0.498               |
| 9     | 0.148       | 0.311       | 0.423       | 0.459       | 0.480       | 0.491       | 0.499             | 0.491               | 0.486               |
| 10    | 0.161       | 0.324       | 0.425       | 0.462       | 0.486       | 0.489       | 0.495             | 0.497               | 0.499               |
| 11    | 0.162       | 0.322       | 0.428       | 0.462       | 0.484       | 0.496       | 0.493             | 0.498               | 0.496               |
| 12    | 0.173       | 0.335       |             | 0.464       | 0.482       | 0.492       | 0.501             | 0.500               | 0.497               |
| 13    | 0.179       | 0.329       |             | 0.465       | 0.482       |             | 0.491             | 0.493               | 0.503               |
| 14    | 0.181       | 0.332       |             | 0.466       |             |             | 0.497             | 0.500               | 0.501               |
| 15    | 0.193       | 0.335       |             | 0.472       |             |             | 0.500             | 0.495               | 0.500               |
| 16    | 0.194       | 0.330       |             |             |             |             | 0.503             | 0.500               |                     |
| 17    | 0.199       | 0.333       |             |             |             |             | 0.500             | 0.503               |                     |
| 18    | 0.198       | 0.339       |             |             |             |             | 0.500             | 0.501               |                     |
| 19    | 0.208       | 0.340       |             |             |             |             |                   | 0.498               |                     |
| 20    | 0.207       | 0.337       |             |             |             |             |                   | 0.501               |                     |
| 21    | 0.209       | 0.348       |             |             |             |             |                   | 0.497               |                     |
| 22    | 0.212       | 0.336       |             |             |             |             |                   |                     |                     |
| 23    | 0.209       | 0.339       |             |             |             |             |                   |                     |                     |
| 24    | 0.216       | 0.342       |             |             |             |             |                   |                     |                     |
| 25    | 0.218       | 0.338       |             |             |             |             |                   |                     |                     |
| 26    | 0.216       | 0.344       |             |             |             |             |                   |                     |                     |
| 27    | 0.213       | 0.340       |             |             |             |             |                   |                     |                     |
| 28    | 0.210       | 0.339       |             |             |             |             |                   |                     |                     |
| 29    | 0.211       | 0.339       |             |             |             |             |                   |                     |                     |
| 30    | 0.208       | 0.341       |             |             |             |             |                   |                     |                     |
| 31    |             | 0.338       |             |             |             |             |                   |                     |                     |
| 32    |             | 0.341       |             |             |             |             |                   |                     |                     |
| 33    |             | 0.333       |             |             |             |             |                   |                     |                     |