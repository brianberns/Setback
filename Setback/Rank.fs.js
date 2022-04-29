
export function Rank_gamePoints(_arg1) {
    switch (_arg1) {
        case 10: {
            return 10;
        }
        case 11: {
            return 1;
        }
        case 12: {
            return 2;
        }
        case 13: {
            return 3;
        }
        case 14: {
            return 4;
        }
        default: {
            return 0;
        }
    }
}

export function PlayingCards_Rank__Rank_get_GamePoints(rank) {
    return Rank_gamePoints(rank);
}

