using UnityEngine;
using System.Collections;
using NyARUnityUtils;
using jp.nyatla.nyartoolkit.cs.markersystem;
using jp.nyatla.nyartoolkit.cs.core;

namespace NyARUnityUtils
{
	public class NyARUnityPSEye :NyARSensor
	{
		private PSEyeTexture _ptx;
	    private NyARUnityRaster _raster;	
		public NyARUnityPSEye(PSEyeTexture i_ptx): base(new NyARIntSize(i_ptx.Width,i_ptx.Height))
		{		
	        this._raster = new NyARUnityRaster(i_ptx.Width,i_ptx.Height,true);
	        base.update(this._raster);
			this._ptx = i_ptx;
		}
	    public void stop()
	    {
	        this._ptx.Stop();
	    }
	    public void start()
	    {
	        this._ptx.Start();
	    }
		public void update()
		{
			this._ptx.Update();
			this._raster.updateByPSEyeTexture(this._ptx);
			base.updateTimeStamp();
			return;
		}
		public override void update(INyARRgbRaster i_input)
		{
			throw new NyARException();
		}
		public void dGetGsTex(Texture2D tx)
		{
			int[] s =(int[])this._gs_raster.getBuffer();
			Color32[] c=new Color32[_ptx.Width * _ptx.Height];
			for(int i = 0; i < _ptx.Height; i++){
				for(int i2=0; i2 < _ptx.Width; i2++){
					int index = i * _ptx.Width + i2;
					c[index].r = c[index].g = c[index].b = (byte)s[index];
					c[index].a = 0xff;
				}
			}
			tx.SetPixels32(c);
			tx.Apply( false );
		}
	}

}

