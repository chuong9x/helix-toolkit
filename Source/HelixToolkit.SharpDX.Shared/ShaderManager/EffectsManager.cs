﻿/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
//#define DEBUGMEMORY
using System;
using System.Collections.Generic;

#if !NETFX_CORE
namespace HelixToolkit.Wpf.SharpDX
#else
namespace HelixToolkit.UWP
#endif
{
    using global::SharpDX.Direct3D;
    using global::SharpDX.Direct3D11;
    using global::SharpDX.DXGI;
    using Shaders;
    using ShaderManager;
    using Core;
    /// <summary>
    /// Shader and Technique manager
    /// </summary>
    public abstract class EffectsManager : DisposeObject, IEffectsManager
    {
        /// <summary>
        /// Occurs when [on dispose resources].
        /// </summary>
        public event EventHandler<bool> OnDisposeResources;
        /// <summary>
        /// The minimum supported feature level.
        /// </summary>
        private const FeatureLevel MinimumFeatureLevel = FeatureLevel.Level_10_0;
        private IDictionary<string, Lazy<IRenderTechnique>> techniqueDict { get; } = new Dictionary<string, Lazy<IRenderTechnique>>();
        /// <summary>
        /// <see cref="IEffectsManager.RenderTechniques"/>
        /// </summary>
        public IEnumerable<string> RenderTechniques { get { return techniqueDict.Keys; } }

        /// <summary>
        /// <see cref="IEffectsManager.ConstantBufferPool"/>
        /// </summary>
        public IConstantBufferPool ConstantBufferPool { get { return constantBufferPool; } }
        private IConstantBufferPool constantBufferPool;

        private IShaderPoolManager shaderPoolManager;
        /// <summary>
        /// <see cref="IEffectsManager.ShaderManager"/>
        /// </summary>
        public IShaderPoolManager ShaderManager { get { return shaderPoolManager; } }

        private IStatePoolManager statePoolManager;

        /// <summary>
        /// <see cref="IEffectsManager.StateManager"/> 
        /// </summary>
        public IStatePoolManager StateManager { get { return statePoolManager; } }

        /// <summary>
        /// Gets the geometry buffer manager.
        /// </summary>
        /// <value>
        /// The geometry buffer manager.
        /// </value>
        public IGeometryBufferManager GeometryBufferManager { get { return geometryBufferManager; } }

        private IGeometryBufferManager geometryBufferManager;

        /// <summary>
        /// Gets the material texture manager.
        /// </summary>
        /// <value>
        /// The material texture manager.
        /// </value>
        public ITextureResourceManager MaterialTextureManager { get { return materialTextureManager; } }
        private ITextureResourceManager materialTextureManager;
        #region 3D Resoruces
        private global::SharpDX.Direct3D11.Device device;
        /// <summary>
        /// 
        /// </summary>
        public global::SharpDX.Direct3D11.Device Device { get { return device; } }
        /// <summary>
        /// 
        /// </summary>
        public DriverType DriverType { private set; get; }

        #endregion
        #region 2D Resources
        private global::SharpDX.Direct2D1.Device device2D;
        /// <summary>
        /// Gets the device2d.
        /// </summary>
        /// <value>
        /// The device2 d.
        /// </value>
        public global::SharpDX.Direct2D1.Device Device2D { get { return device2D; } }


        private global::SharpDX.Direct2D1.DeviceContext deviceContext2D;
        /// <summary>
        /// Gets or sets the device2 d context.
        /// </summary>
        /// <value>
        /// The device2 d context.
        /// </value>
        public global::SharpDX.Direct2D1.DeviceContext DeviceContext2D { get { return deviceContext2D; } }
        /// <summary>
        /// Gets the factory2 d.
        /// </summary>
        /// <value>
        /// The factory2 d.
        /// </value>
        public global::SharpDX.Direct2D1.Factory1 Factory2D { get { return factory2D; } }
        private global::SharpDX.Direct2D1.Factory1 factory2D;

        private global::SharpDX.WIC.ImagingFactory wicImgFactory;
        /// <summary>
        /// Gets the wic img factory.
        /// </summary>
        /// <value>
        /// The wic img factory.
        /// </value>
        public global::SharpDX.WIC.ImagingFactory WICImgFactory { get { return wicImgFactory; } }

        private global::SharpDX.DirectWrite.Factory directWriteFactory;
        /// <summary>
        /// Gets the direct write factory.
        /// </summary>
        /// <value>
        /// The direct write factory.
        /// </value>
        public global::SharpDX.DirectWrite.Factory DirectWriteFactory { get { return directWriteFactory; } }
        #endregion
        /// <summary>
        /// 
        /// </summary>
        public int AdapterIndex { private set; get; }
        /// <summary>
        /// 
        /// </summary>
        public bool Initialized { private set; get; } = false;
        /// <summary>
        /// Initializes a new instance of the <see cref="EffectsManager"/> class.
        /// </summary>
        public EffectsManager()
        {
            Initialize();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="EffectsManager"/> class.
        /// </summary>
        /// <param name="adapterIndex">Index of the adapter.</param>
        public EffectsManager(int adapterIndex)
        {
            Initialize(adapterIndex);
        }

        protected void Initialize()
        {
#if DEBUGMEMORY
            global::SharpDX.Configuration.EnableObjectTracking = true;
#endif

            int adapterIndex = -1;
            using (var adapter = GetBestAdapter(out adapterIndex))
            {
                Initialize(adapterIndex);
            }
        }
        /// <summary>
        /// Initializes this instance.
        /// </summary>
        protected void Initialize(int adapterIndex)
        {
            if (Initialized)
            { return; }
            var adapter = GetAdapter(ref adapterIndex);
            AdapterIndex = adapterIndex;
#if DX11
            if (adapter != null)
            {
                if (adapter.Description.VendorId == 0x1414 && adapter.Description.DeviceId == 0x8c)
                {
                    DriverType = DriverType.Warp;
                    device = new global::SharpDX.Direct3D11.Device(adapter, DeviceCreationFlags.BgraSupport, FeatureLevel.Level_10_0);
                }
                else
                {
                    DriverType = DriverType.Hardware;
#if DEBUGMEMORY
                    device = new global::SharpDX.Direct3D11.Device(adapter, DeviceCreationFlags.BgraSupport | DeviceCreationFlags.Debug);
#else
                    device = new global::SharpDX.Direct3D11.Device(adapter, DeviceCreationFlags.BgraSupport);
#endif
                    // DeviceCreationFlags.Debug should not be used in productive mode!
                    // See: http://sharpdx.org/forum/4-general/1774-how-to-debug-a-sharpdxexception
                    // See: http://stackoverflow.com/questions/19810462/launching-sharpdx-directx-app-with-devicecreationflags-debug
                }
            }
#else
            device = new global::SharpDX.Direct3D11.Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport, FeatureLevel.Level_10_1);
#endif
            
#region Initial Internal Pools
            RemoveAndDispose(ref constantBufferPool);
            constantBufferPool = Collect(new ConstantBufferPool(Device));

            RemoveAndDispose(ref shaderPoolManager);
            shaderPoolManager = Collect(new ShaderPoolManager(Device, constantBufferPool));

            RemoveAndDispose(ref statePoolManager);
            statePoolManager = Collect(new StatePoolManager(Device));

            RemoveAndDispose(ref geometryBufferManager);
            geometryBufferManager = Collect(new GeometryBufferManager());

            RemoveAndDispose(ref materialTextureManager);
            materialTextureManager = Collect(new Model.TextureResourceManager(Device));
#endregion
#region Initial Techniques
            var techniqueDescs = LoadTechniqueDescriptions();
            foreach(var tech in techniqueDescs)
            {
                AddTechnique(tech);
            }
            #endregion

            factory2D = Collect(new global::SharpDX.Direct2D1.Factory1(global::SharpDX.Direct2D1.FactoryType.MultiThreaded));
            wicImgFactory = Collect(new global::SharpDX.WIC.ImagingFactory());
            directWriteFactory = Collect(new global::SharpDX.DirectWrite.Factory(global::SharpDX.DirectWrite.FactoryType.Shared));
            using (var dxgiDevice2 = device.QueryInterface<global::SharpDX.DXGI.Device>())
            {
                device2D = Collect(new global::SharpDX.Direct2D1.Device(factory2D, dxgiDevice2));
                deviceContext2D = Collect(new global::SharpDX.Direct2D1.DeviceContext(device2D, global::SharpDX.Direct2D1.DeviceContextOptions.EnableMultithreadedOptimizations));
            }

            Initialized = true;
        }
        /// <summary>
        /// <see cref="IEffectsManager.AddTechnique(TechniqueDescription)"/>
        /// </summary>
        /// <param name="description"></param>
        public void AddTechnique(TechniqueDescription description)
        {
            if (techniqueDict.ContainsKey(description.Name))
            {
                throw new ArgumentException($"Technique { description.Name } already exists.");
            }
            techniqueDict.Add(description.Name, new Lazy<IRenderTechnique>(() => { return Initialized ? Collect(new Technique(description, Device, this)) : null; }, true));
        }
        /// <summary>
        /// <see cref="IEffectsManager.RemoveTechnique(string)"/>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool RemoveTechnique(string name)
        {
            Lazy<IRenderTechnique> t;
            techniqueDict.TryGetValue(name, out t);
            if (t != null && t.IsValueCreated)
            {
                var v = t.Value;
                RemoveAndDispose(ref v);
            }
            return techniqueDict.Remove(name);
        }
        /// <summary>
        /// Loads the technique descriptions.
        /// </summary>
        /// <returns></returns>
        protected abstract IList<TechniqueDescription> LoadTechniqueDescriptions();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private static Adapter GetBestAdapter(out int bestAdapterIndex)
        {
            using (var f = new Factory1())
            {
                Adapter bestAdapter = null;
                bestAdapterIndex = -1;
                int adapterIndex = -1;
                ulong bestVideoMemory = 0;
                ulong bestSystemMemory = 0;

                foreach (var item in f.Adapters)
                {
                    adapterIndex++;

                    // not skip the render only WARP device
                    if (item.Description.VendorId != 0x1414 || item.Description.DeviceId != 0x8c)
                    {
                        // Windows 10 fix
                        if (item.Outputs == null || item.Outputs.Length == 0)
                        {
                            continue;
                        }
                    }

                    var level = global::SharpDX.Direct3D11.Device.GetSupportedFeatureLevel(item);

                    if (level < MinimumFeatureLevel)
                    {
                        continue;
                    }

                    ulong videoMemory = item.Description.DedicatedVideoMemory.ToUInt64();
                    ulong systemMemory = item.Description.DedicatedSystemMemory.ToUInt64();

                    if ((bestAdapter == null) || (videoMemory > bestVideoMemory) || ((videoMemory == bestVideoMemory) && (systemMemory > bestSystemMemory)))
                    {
                        bestAdapter = item;
                        bestAdapterIndex = adapterIndex;
                        bestVideoMemory = videoMemory;
                        bestSystemMemory = systemMemory;
                    }
                }

                return bestAdapter;
            }
        }

        private static Adapter GetAdapter(ref int index)
        {
            using (var f = new Factory1())
            {
                if (f.Adapters.Length <= index)
                {
                    return GetBestAdapter(out index);
                }
                else
                {
                    return f.Adapters[index];
                }
            }
        }
        /// <summary>
        /// Gets the technique.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Manager has not been initialized.</exception>
        /// <exception cref="ArgumentException"></exception>
        public IRenderTechnique GetTechnique(string name)
        {
            if (!Initialized)
            {
                throw new Exception("Manager has not been initialized.");
            }
            Lazy<IRenderTechnique> t;
            techniqueDict.TryGetValue(name, out t);
            if (t == null)
            {
                throw new ArgumentException($"Technique {name} does not exist.");
            }
            return t.Value;
        }
        /// <summary>
        /// Gets the <see cref="IRenderTechnique"/> with the specified name.
        /// </summary>
        /// <value>
        /// The <see cref="IRenderTechnique"/>.
        /// </value>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public IRenderTechnique this[string name]
        {
            get
            {
                return GetTechnique(name);
            }           
        }

        /// <summary>
        /// <see cref="DisposeObject.Dispose(bool)"/>
        /// </summary>
        /// <param name="disposeManagedResources"></param>
        protected override void Dispose(bool disposeManagedResources)
        {
            OnDisposeResources?.Invoke(this, true);
            techniqueDict.Clear();
            base.Dispose(disposeManagedResources);
            Initialized = false;
            Disposer.RemoveAndDispose(ref device);
#if DEBUGMEMORY
            ReportResources();
#endif
        }

#region Handle Device Error        
        /// <summary>
        /// Called when [device error].
        /// </summary>
        public void OnDeviceError()
        {
            techniqueDict.Clear();
            OnDisposeResources?.Invoke(this, true);
            this.DisposeAndClear();
            Disposer.RemoveAndDispose(ref device);
            Initialized = false;
#if DEBUGMEMORY
            ReportResources();
#endif
            Initialize(AdapterIndex);
        }
#endregion

#if DEBUGMEMORY
        protected void ReportResources()
        {
            Console.WriteLine(global::SharpDX.Diagnostics.ObjectTracker.ReportActiveObjects());
            var liveObjects = global::SharpDX.Diagnostics.ObjectTracker.FindActiveObjects();
            Console.WriteLine($"Live object count = {liveObjects.Count}");
            //if (liveObjects.Count != 0)
            //{
            //    foreach(var obj in liveObjects)
            //    {
            //        Console.WriteLine(obj.ToString());
            //    }
            //}
        }
#endif
    }


}
