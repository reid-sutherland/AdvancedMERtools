namespace AdvancedMERTools;

public class GroovyNoise : AMERTInteractable
{
    public new GNDTO Base { get; set; }

    protected virtual void Start()
    {
        Base = base.Base as GNDTO;
        AdvancedMERTools.Singleton.GroovyNoises.Add(this);
        //MEC.Timing.CallDelayed(0.1f, () => 
        //{
        //    if (AdvancedMERTools.Singleton.groovyNoises.All(x => x.Base.GMDTOs.Select(y => y.codes).All(y => !y.Contains(Base.Code))))
        //        Active = true;
        //});
    }

    protected virtual void Update()
    {
        if (Active)
        {
            //ServerConsole.AddLog("!!!");
            GMDTO.Execute(Base.Settings, new ModuleGeneralArguments { Schematic = OSchematic, Transform = transform });
        }
        Active = false;
    }
}

public class FGroovyNoise : GroovyNoise
{
    public new FGNDTO Base { get; set; }

    protected override void Start()
    {
        Base = ((AMERTInteractable)this).Base as FGNDTO;
        AdvancedMERTools.Singleton.GroovyNoises.Add(this);
        //MEC.Timing.CallDelayed(0.1f, () => 
        //{
        //    if (AdvancedMERTools.Singleton.groovyNoises.All(x => x.Base.GMDTOs.Select(y => y.codes).All(y => !y.Contains(Base.Code))))
        //        Active = true;
        //});
    }

    protected override void Update()
    {
        if (Active)
        {
            //ServerConsole.AddLog("!!!");
            FGMDTO.Execute(Base.Settings, new FunctionArgument(this));
        }
        Active = false;
    }
}
