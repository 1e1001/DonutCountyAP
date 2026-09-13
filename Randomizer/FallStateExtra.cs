using DonutCountyAP.Generated;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DonutCountyAP.Randomizer;

public class FallStateExtra : MonoBehaviour
{
    // TODO: use unity event things if they can automatically unsubscribe on destroy
    static readonly Dictionary<ItemId, HashSet<FallStateExtra>> _onItem = [];
    static readonly Dictionary<int, HashSet<FallStateExtra>> _onLocation = [];
    static readonly HashSet<FallStateExtra> _all = [];

    public ItemId Unlock;
    // corresponds to a Trash or TrashType location depending on game options
    public int Location;
    public int DebugIndex;
    bool _unlocked;
    bool _found;
    Rigidbody _body;
    readonly List<Collider> _colliders = [];
    readonly List<KeyValuePair<Renderer, Material[]>> _meshes = [];
    GameObject _marker;

    public void Refresh()
    {
        _unlocked = Unlock == ItemId.None || Plugin.GameState.Has(Unlock);
        _found = Location == -1 || Plugin.GameState.HasLocation(Location);
        _all.Add(this);
        if (!_unlocked)
        {
            if (!_onItem.TryGetValue(Unlock, out var value))
                _onItem.Add(Unlock, value = []);
            value.Add(this);
            SetMaterial();
            _body = GetComponent<Rigidbody>();
            _body.constraints = RigidbodyConstraints.FreezeAll;
        } else
        {
            ClearMaterial();
            if (_body != null)
                _body.constraints = RigidbodyConstraints.None;
            if (_colliders != null)
                foreach (var collider in _colliders)
                    collider.enabled = true;
            _body = null;
        }
        if (_marker != null)
        {
            Destroy(_marker);
            _marker = null;
        }
        if (!_found)
        {
            if (!_onLocation.TryGetValue(Location, out var value))
                _onLocation.Add(Location, value = []);
            value.Add(this);
            if (!Plugin.Options.TrashTrackers)
                return;
            _marker = GameObject.Instantiate(PackedAssets.Marker);
            _marker.transform.SetParent(transform);
            _marker.transform.localPosition = new Vector3();
        }
    }

    void Update()
    {
        // TODO: don't
        if (!_unlocked && _colliders != null)
            foreach (var collider in _colliders)
                collider.enabled = false;
    }

    public static void OnItem(ItemId id)
    {
        if (id == ItemId.DebugToggle)
        {
            var unlock = Plugin.GameState.Quantity(ItemId.DebugToggle) % 2 == 1;
            foreach (var listener in _all)
                listener.DebugToggle(unlock);
        }
        if (!_onItem.TryGetValue(id, out var listeners))
            return;
        _onItem.Remove(id);
        foreach (var listener in listeners)
            listener.OnItem();
    }
    public static void OnLocation(int id)
    {
        if (!_onLocation.TryGetValue(id, out var listeners))
            return;
        _onLocation.Remove(id);
        foreach (var listener in listeners)
            listener.OnLocation();
    }

    void OnItem()
    {
        _unlocked = true;
        ClearMaterial();
        if (_body != null)
            _body.constraints = RigidbodyConstraints.None;
        if (_colliders != null)
            foreach (var collider in _colliders)
                collider.enabled = true;
    }
    void OnLocation()
    {
        _found = true;
        if (_marker == null)
            return;
        Destroy(_marker);
        _marker = null;
    }

    void DebugToggle(bool unlock)
    {
        // no clue...
        if (this == null)
            return;
        if (unlock)
            OnItem();
        else
            Refresh();
    }
    public static KeyValuePair<Transform, string>[] DebugMarkers()
    {
        var markers = new KeyValuePair<Transform, string>[_all.Count()];
        var i = 0;
        foreach (var listener in _all)
            markers[i++] = listener == null ? new(null, "") :new(listener.transform, $"[{listener.DebugIndex}] {listener.name}");
        return markers;
    }

    static void SetMaterial(Renderer mesh)
    {
        var count = mesh.sharedMaterials.Length;
        var array = new Material[count];
        // TODO: figure out what specific object is fucked up, so this is less wasteful
        for (var i = 0; i < count; ++i)
            array[i] = Object.Instantiate(PackedAssets.TrashLocked);
        mesh.sharedMaterials = array;
    }
    // TODO: what is setting my meshes to null!!
    void SetMaterial()
    {
        foreach (var kv in _meshes)
            if (kv.Key != null)
                SetMaterial(kv.Key);
    }
    void ClearMaterial()
    {
        foreach (var kv in _meshes)
            if (kv.Key != null)
                kv.Key.sharedMaterials = kv.Value;
    }

    public void AddChildRenderer(Renderer mesh)
    {
        if (_unlocked)
            return;
        if (mesh.name == "aptrashmarker(Clone)")
            return;
        _meshes.Add(new(mesh, mesh.sharedMaterials));
        SetMaterial(mesh);
    }
    public void AddChildCollider(Collider collider)
    {
        if (_unlocked)
            return;
        // don't accidentally reenable dead colliders
        if (!collider.enabled)
            return;
        _colliders.Add(collider);
        collider.enabled = false;
    }

    void OnDestroy()
    {
        if (_onItem.ContainsKey(Unlock))
            _onItem[Unlock].Remove(this);
        if (_onLocation.ContainsKey(Location))
            _onLocation[Location].Remove(this);
       _all.Remove(this);
    }

    public void Collect()
    {
        if (Plugin.GameState.Quantity(ItemId.DebugToggle) % 2 == 1)
            return;
        if (Location != -1)
            Plugin.GameState.ReceivedLocation(Location, true);
    }

}

