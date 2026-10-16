from pathlib import Path
r=Path(__file__).resolve().parents[3]
p=r/'UnityProject/Assets/Game/Runtime/Campaign/RogueliteMapRun.cs'
s=p.read_text('utf-8-sig')
s=s.replace('Math.Min(UnitState.HeroBaseHealth, rogueRunDto.CurrentHealth + choice.HealthGain)','Math.Min(RogueHealthCapacity, rogueRunDto.CurrentHealth + choice.HealthGain)')
s=s.replace('Math.Min(UnitState.HeroBaseHealth, CurrentHealth + 6)','Math.Min(RogueHealthCapacity, CurrentHealth + 6)')
s=s.replace('Math.Min(UnitState.HeroBaseHealth, rogueRunDto.CurrentHealth + UnitState.HeroBaseHealth / 2)','Math.Min(RogueHealthCapacity, rogueRunDto.CurrentHealth + RogueHealthCapacity / 2)')
s=s.replace('Math.Max(1, Math.Min(UnitState.HeroBaseHealth, rogueRunDto.CurrentHealth))','Math.Max(1, Math.Min(RogueHealthCapacity, rogueRunDto.CurrentHealth))')
s=s.replace('Math.Max(1, Math.Min(UnitState.HeroBaseHealth, CurrentHealth))','Math.Max(1, Math.Min(RogueHealthCapacity, CurrentHealth))')
p.write_text(s,'utf-8')
p=r/'UnityProject/Assets/Game/Runtime/Presentation/FormalRogueliteUi.cs';s=p.read_text('utf-8-sig')
a=s.index('        private void DrawFirstRunMedical(');b=s.index('        private void MedicalMetricRow(',a)
s=s[:a]+s[a:b].replace('UnitState.HeroBaseHealth','run.RogueHealthCapacity')+s[b:]
p.write_text(s,'utf-8')
