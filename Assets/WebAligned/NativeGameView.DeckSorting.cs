using System.Linq;
using UnityEngine;

namespace PersonaCards.WebAligned
{
    public sealed partial class NativeGameView
    {
        void RenderDeckSorting(RectTransform panel)
        {
            Label(panel,"共 "+state["deckCards"].Count()+" 张",60,199,300,36,23,Muted);
            Label(panel,"牌库排序",745,201,180,35,21,Muted);
            var rank=Btn(panel,"大小",940,193,145,48,()=>Act("click","#deck-sort-rank"),S(state["deckSortMode"])=="rank");rank.name="牌库大小";
            var suit=Btn(panel,"花色",1100,193,145,48,()=>Act("click","#deck-sort-suit"),S(state["deckSortMode"])=="suit");suit.name="牌库花色";
        }
    }
}

