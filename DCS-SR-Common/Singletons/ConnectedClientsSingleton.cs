using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Caliburn.Micro;
using Ciribob.DCS.SimpleRadio.Standalone.Common.Network;

namespace Ciribob.DCS.SimpleRadio.Standalone.Common.Singletons;
using PropertyChangedBase = Ciribob.SRS.Common.Helpers.PropertyChangedBase;
public sealed class ConnectedClientsSingleton : PropertyChangedBase
{
    private static volatile ConnectedClientsSingleton _instance;
    private static readonly object _lock = new();

    private ConnectedClientsSingleton()
    {
    }

    public ConcurrentDictionary<string, SRClient> Clients { get; } = new();

    public static ConnectedClientsSingleton Instance
    {
        get
        {
            if (_instance == null)
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new ConnectedClientsSingleton();
                }

            return _instance;
        }
    }

    public SRClient this[string key]
    {
        get => Clients[key];
        set
        {
            Clients[key] = value;
            NotifyAll();
        }
    }

    public ICollection<SRClient> Values => Clients.Values;


    public int Total => Clients.Count();

    public event PropertyChangedEventHandler PropertyChanged;

    public void NotifyAll()
    {
        NotifyPropertyChanged("Total");
    }

    public bool TryRemove(string key, out SRClient value)
    {
        var result = Clients.TryRemove(key, out value);
        if (result) NotifyPropertyChanged("Total");
        return result;
    }

    public void Clear()
    {
        Clients.Clear();
        NotifyPropertyChanged("Total");
    }

    public bool TryGetValue(string key, out SRClient value)
    {
        return Clients.TryGetValue(key, out value);
    }

    public bool ContainsKey(string key)
    {
        return Clients.ContainsKey(key);
    }
}