using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public sealed class AcademyMealDefinition
    {
        public string Id {get;} public string Name {get;} public string Currency {get;} public int Cost {get;} public string Effect {get;} public string IconPath {get;}
        public string CostText=>Cost+" "+(Currency=="gold"?"金币":Currency=="contribution"?"学院贡献":"学院食材");
        public AcademyMealDefinition(string id,string name,string currency,int cost,string effect,string icon)
        {Id=id;Name=name;Currency=currency;Cost=cost;Effect=effect;IconPath=icon;}
    }
    // Product source: master plan2.1.3. Tutorial offers remain preset; academy offers are seeded.
    public static class AcademyMealCatalog
    {
        public static readonly IReadOnlyList<AcademyMealDefinition> All=new[]{
            new AcademyMealDefinition("MEAL-POWER","强攻炖肉","gold",3,"武器直接伤害 +2",FormalArtRegistry.CommandPath("attack")),
            new AcademyMealDefinition("MEAL-AETHER","导能热饮","contribution",1,"个人魔力上限 +2；进入战斗时恢复满",FormalArtRegistry.ResourceMetricPath("mana")),
            new AcademyMealDefinition("MEAL-GUARD","岩盐浓汤","food",1,"每场战斗开始获得 6 护盾",FormalArtRegistry.ResourceMetricPath("shield")),
            new AcademyMealDefinition("MEAL-MOVE","疾行烤串","gold",3,"每个自己回合第一次移动的步数 +1",FormalArtRegistry.CommandPath("move")),
            new AcademyMealDefinition("MEAL-SPELL","节流茶","contribution",1,"每场战斗第一次施放术式时魔力成本 -1，最低为 0",FormalArtRegistry.ResourceMetricPath("operational_aether")),
            new AcademyMealDefinition("MEAL-VITAL","坚韧面包","food",1,"最大生命 +4；每场战斗开始恢复 4 生命",FormalArtRegistry.ResourceMetricPath("health")),
            new AcademyMealDefinition("MEAL-HUNTER","猎团香汤","gold",4,"每场战斗第一次武器命中额外造成 4 伤害",FormalArtRegistry.CommandPath("attack")),
            new AcademyMealDefinition("MEAL-CLEAR","清醒粥","food",1,"每场战斗第一次受到负面状态时，持续时间 -1 回合，最低为 1 回合",FormalArtRegistry.ResourceMetricPath("notice"))};
        public static AcademyMealDefinition Get(string id)=>All.SingleOrDefault(x=>x.Id==id)??throw new InvalidOperationException("Unknown academy meal: "+id);
        public static string[] Candidates(int seed,string nodeId)
        {
            int hash=seed;unchecked{foreach(char c in nodeId??string.Empty)hash=hash*31+c;}
            var random=new Random(hash);return All.OrderBy(x=>random.Next()).Take(3).Select(x=>x.Id).ToArray();
        }
        public static bool Affordable(RogueliteMapRun run,string id)
        {var meal=Get(id);return (meal.Currency=="gold"?run.Gold:meal.Currency=="contribution"?run.StageContribution:run.AcademyFoodCount)>=meal.Cost;}
    }
}
