namespace BBRAPIModules;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class ModuleAttribute : Attribute
{
    public string Description { get; set; }
    public string Version { get; set; }

    public ModuleAttribute(string description, string version)
    {
        this.Description = description;
        this.Version = version;
    }
}
