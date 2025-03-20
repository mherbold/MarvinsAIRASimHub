﻿
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Windows.Media;

using GameReaderCommon;

using SimHub.Plugins;

namespace MarvinsAIRASimHub
{
	[StructLayout( LayoutKind.Sequential, Pack = 4 )]
	public struct TelemetryData
	{
		public int tickCount;

		public float wheelMax;
		public float overallScale;
		public bool overallScaleAutoReady;
		public float overallScaleAutoClipLimit;
		public float detailScale;
		public float parkedScale;
		public float frequency;

		public int understeerFxStyle;
		public float understeerFxStrength;
		public float understeerFxCurve;
		public float understeerYRFactorLStart;
		public float understeerYRFactorLEnd;
		public float understeerYRFactorRStart;
		public float understeerYRFactorREnd;

		public int oversteerFxStyle;
		public float oversteerFxStrength;
		public float oversteerFxCurve;
		public float oversteerYVelocityStart;
		public float oversteerYVelocityEnd;

		public float lfeScale;

		public float ffbInAmount;
		public int ffbOutAmount;
		public bool ffbClipping;
		public float yawRateFactor;
		public float gForce;
		public float understeerAmount;
		public float oversteerAmount;
		public bool crashProtectionEngaged;
	}

	[PluginDescription( "Marvin's Awesome iRacing App Data Plugin" )]
	[PluginAuthor( "Marvin Herbold" )]
	[PluginName( "MAIRA Data Plugin" )]
	public class MarvinsAIRADataPlugin : IPlugin, IDataPlugin, IWPFSettingsV2
	{
		private const string TELEMETRY_MEMORYMAPPEDFILENAME = "Local\\MAIRATelemetry";

		public PluginManager PluginManager { get; set; }

		public ImageSource PictureIcon => this.ToIcon( Properties.Resources.icon );

		public string LeftMenuTitle => "MAIRA data plugin";

		private TelemetryData telemetryData = new TelemetryData();
		private MemoryMappedFile memoryMappedFile = null;
		private MemoryMappedViewAccessor memoryMappedFileViewAccessor = null;
		private bool faulted = false;
		private bool connected = false;
		private int nextAttempt = 0;

		public void Init( PluginManager pluginManager )
		{
			SimHub.Logging.Current.Info( "Starting MAIRA data plugin" );

			this.AttachDelegate( name: "Faulted", valueProvider: () => faulted );
			this.AttachDelegate( name: "Connected", valueProvider: () => connected );

			this.AttachDelegate( name: "TickCount", valueProvider: () => telemetryData.tickCount );

			this.AttachDelegate( name: "WheelMax", valueProvider: () => telemetryData.wheelMax );
			this.AttachDelegate( name: "OverallScale", valueProvider: () => telemetryData.overallScale );
			this.AttachDelegate( name: "OverallScaleAutoReady", valueProvider: () => telemetryData.overallScaleAutoReady );
			this.AttachDelegate( name: "OverallScaleAutoClipLimit", valueProvider: () => telemetryData.overallScaleAutoClipLimit );
			this.AttachDelegate( name: "DetailScale", valueProvider: () => telemetryData.detailScale );
			this.AttachDelegate( name: "ParkedScale", valueProvider: () => telemetryData.parkedScale );
			this.AttachDelegate( name: "Frequency", valueProvider: () => telemetryData.frequency );

			this.AttachDelegate( name: "UndersteerFxStyle", valueProvider: () => telemetryData.understeerFxStyle );
			this.AttachDelegate( name: "UndersteerFxStrength", valueProvider: () => telemetryData.understeerFxStrength );
			this.AttachDelegate( name: "UndersteerFxCurve", valueProvider: () => telemetryData.understeerFxCurve );
			this.AttachDelegate( name: "UndersteerYRFactorLStart", valueProvider: () => telemetryData.understeerYRFactorLStart );
			this.AttachDelegate( name: "UndersteerYRFactorLEnd", valueProvider: () => telemetryData.understeerYRFactorLEnd );
			this.AttachDelegate( name: "UndersteerYRFactorRStart", valueProvider: () => telemetryData.understeerYRFactorRStart );
			this.AttachDelegate( name: "UndersteerYRFactorREnd", valueProvider: () => telemetryData.understeerYRFactorREnd );

			this.AttachDelegate( name: "OversteerFxStyle", valueProvider: () => telemetryData.oversteerFxStyle );
			this.AttachDelegate( name: "OversteerFxStrength", valueProvider: () => telemetryData.oversteerFxStrength );
			this.AttachDelegate( name: "OversteerFxCurve", valueProvider: () => telemetryData.oversteerFxCurve );
			this.AttachDelegate( name: "OversteerYVelocityStart", valueProvider: () => telemetryData.oversteerYVelocityStart );
			this.AttachDelegate( name: "OversteerYVelocityEnd", valueProvider: () => telemetryData.oversteerYVelocityEnd );

			this.AttachDelegate( name: "LFEScale", valueProvider: () => telemetryData.lfeScale );

			this.AttachDelegate( name: "FFNInAmount", valueProvider: () => telemetryData.ffbInAmount );
			this.AttachDelegate( name: "FFBOutAmount", valueProvider: () => telemetryData.ffbOutAmount );
			this.AttachDelegate( name: "YawRateFactor", valueProvider: () => telemetryData.yawRateFactor );
			this.AttachDelegate( name: "GForce", valueProvider: () => telemetryData.gForce );
			this.AttachDelegate( name: "UndersteerAmount", valueProvider: () => telemetryData.understeerAmount );
			this.AttachDelegate( name: "OversteerAmount", valueProvider: () => telemetryData.oversteerAmount );
			this.AttachDelegate( name: "CrashProtectionEngaged", valueProvider: () => telemetryData.crashProtectionEngaged );
		}

		public void End( PluginManager pluginManager )
		{
		}

		public void DataUpdate( PluginManager pluginManager, ref GameData data )
		{
			if ( faulted )
			{
				return;
			}

			try
			{
				if ( memoryMappedFile == null )
				{
					if ( Environment.TickCount >= nextAttempt )
					{
						try
						{
							memoryMappedFile = MemoryMappedFile.OpenExisting( TELEMETRY_MEMORYMAPPEDFILENAME );
						}
						catch ( FileNotFoundException )
						{
							nextAttempt = Environment.TickCount + 5000;
						}
					}
				}

				if ( memoryMappedFile != null )
				{
					if ( memoryMappedFileViewAccessor == null )
					{
						memoryMappedFileViewAccessor = memoryMappedFile.CreateViewAccessor();

						connected = true;
					}

					memoryMappedFileViewAccessor?.Read( 0, out telemetryData );
				}
			}
			catch
			{
				faulted = true;
			}
		}

		public System.Windows.Controls.Control GetWPFSettingsControl( PluginManager pluginManager )
		{
			return null;
		}
	}
}
