using System.Linq;

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
        //    if (AdvancedMERTools.Singleton.GroovyNoises.All(x => x.Base.Settings.Select(y => y.Targets).All(y => !y.Contains(Base.Code))))
        //    {
        //        Log.Debug($"Added groovy noise: {gameObject.name} ({OSchematic.Name})");
        //        Active = true;
        //    }
        //});
    }

    protected virtual void Update()
    {
        if (Active)
        {
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
        //    if (AdvancedMERTools.Singleton.GroovyNoises.All(x => x.Base.Settings.Select(y => y.Targets).All(y => !y.Contains(Base.Code))))
        //    {
        //        Active = true;
        //    }
        //});
    }

    protected override void Update()
    {
        if (Active)
        {
            FGMDTO.Execute(Base.Settings, new FunctionArgument(this));
        }
        Active = false;
    }
}
