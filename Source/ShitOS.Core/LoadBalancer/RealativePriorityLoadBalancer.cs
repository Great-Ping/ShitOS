using ShitOS.Core.Command;
using ShitOS.Core.Task;

namespace ShitOS.Core.LoadBalancer;

public class RealativePriorityLoadBalancer : IOsLoadBalancer
{
    private readonly List<OsTask> _tasks;
    private readonly IDictionary<int, OsTask?> _executableTasks;
    
    public RealativePriorityLoadBalancer()
    {
        _tasks = new List<OsTask>();
        _executableTasks = new Dictionary<int, OsTask?>();
    }
    
    /// <inheritdoc/>
    public IEnumerable<OsTask> InterruptedTasks => _tasks
        .Where(x => x.State is OsTaskState.Interrupted);
    
    #pragma warning disable
    /// <inheritdoc/>
    public IEnumerable<OsTask> Tasks => _tasks;
    
    #pragma warning restore
    
    /// <inheritdoc/>
    public void AddTask(OsTask task)
    {
        _tasks.Add(task);
        _tasks.Sort((x, y) => y.Priority - x.Priority);
    }
    
    /// <inheritdoc/>
    public int GetMaxTicsInRound(int totalTics)
    {
        return 10;
    }

    public void RemoveTask(OsTask task)
    {
        _tasks.Remove(task);
        
        var keyValue = _executableTasks.FirstOrDefault(x => x.Value == task);
        if  (keyValue.Value != null)
            _executableTasks.Remove(keyValue.Key);
    }


    /// <inheritdoc/>
    public OsTask? SelectTaskOrDefault(int cpuIndex)
    {
        if ( _tasks.Count == 0)
            return null;

        _executableTasks.TryGetValue(
            cpuIndex, 
            out OsTask? selectedTask
        );
        
        selectedTask = SelectTaskOrDefault(cpuIndex, selectedTask);
        _executableTasks[cpuIndex] = selectedTask;
        
        return selectedTask;
    }
    
    
    private OsTask? SelectTaskOrDefault(int cpuIndex, OsTask? selectedTask)
    {
        //Выбираем новую исполняемую таску
        if (selectedTask == null)
        { 
            selectedTask = SelectNextTask(cpuIndex);
            selectedTask?.OnTaskSelected();
        }

        //Таска может уйти в прерывание сразу после выбора
        if (selectedTask?.State is OsTaskState.Completed or OsTaskState.Interrupted)
        {
            selectedTask = SelectNextTask(cpuIndex);
            selectedTask?.OnTaskSelected();
            selectedTask = SelectTaskOrDefault(cpuIndex, selectedTask);
        }
        
        return selectedTask;
    }

    private OsTask? SelectNextTask(int cpuIndex)
    {
        return _tasks
            .FirstOrDefault(task => task.State is OsTaskState.Waiting);
    }
}