/************************************************************************************
 Copyright: Copyright 2014 Beijing Noitom Technology Ltd. All Rights reserved.
 Pending Patents: PCT/CN2014/085659 PCT/CN2014/071006

 Licensed under the Perception Neuron SDK License Beta Version (the “License");
 You may only use the Perception Neuron SDK when in compliance with the License,
 which is provided at the time of installation or download, or which
 otherwise accompanies this software in the form of either an electronic or a hard copy.

 A copy of the License is included with this package or can be obtained at:
 http://www.neuronmocap.com

 Unless required by applicable law or agreed to in writing, the Perception Neuron SDK
 distributed under the License is provided on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing conditions and
 limitations under the License.
************************************************************************************/

using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using NeuronDataReaderWraper;
using Neuron;

namespace Neuron
{

	public struct StampTag
	{
		public string name;
		public int startStamp, endStamp;
		public StampTag(string s, int t)
		{
			name = s;
			startStamp = t;
			endStamp = -1;
		}
	}
	// cache motion data and parse to animator
	public class NeuronActor
	{
		public static int MaxFrameDataLength
		{
			get { return ( (int)NeuronBones.NumOfBones + 1 ) * 6; }
		}

        public static int MaxCalcDataLength
        {
            get { return ( (int)CalcBones.NumOfBones ) * 16 + 2; }
        }

		UserData user;
	    
		public delegate bool 							NoFrameDataDelegate();
		public delegate bool							ResumeFrameDataDelegate();
		
		static float									NeuronUnityLinearScale = 0.01f;

        bool                                            isRecording = false;
		public bool										isReading = false; 
		BvhDataHeader									bvhHeader;
        CalcDataHeader                                  calcHeader;
		float[]											data = new float[MaxFrameDataLength];
		float[] 										recData = new float[MaxFrameDataLength];
        List<KeyValuePair<int, float[]>>                bvhDataRecord;
        List<KeyValuePair<int, float[]>>                calcDataRecord;
		List<NoFrameDataDelegate>						noFrameDataCallbacks = new List<NoFrameDataDelegate>();
		List<ResumeFrameDataDelegate>					resumeFrameDataCallbacks = new List<ResumeFrameDataDelegate>();
		public List<StampTag>							tagRecord;	

		bool											isPlayback = false;
		bool											isPause = false;
		public int 										playbackTimeStamp = 0;
		public List<KeyValuePair<int, float[]>>			recKvp;
		List<int>										calcRecStamp;
		public int										playCount = 0;

		public List<StampTag>							readTagKvp;
		
		public Guid										guid = Guid.NewGuid();
		public NeuronSource								owner = null;
		public float[]									boneSizes = new float[(int)NeuronBones.NumOfBones];
		
		public int										actorID { get; private set; }
		public DataVersion								version { get { return bvhHeader.DataVersion; } }
		public string 									name { get { return bvhHeader.AvatarName; } }
		public int										index { get { return (int)bvhHeader.AvatarIndex; } }
		public bool										withDisplacement { get { return bvhHeader.bWithDisp != 0; } }
		public bool										withReference { get { return bvhHeader.bWithReference != 0; } }
		public int										dataCount { get { return (int)bvhHeader.DataCount; } }
		public int										bvhtimeStamp = 0;
		public int										calctimeStamp = 0;
		StampTag 										tag;

		public void RegisterNoFrameDataCallback( NoFrameDataDelegate callback )
		{
			if( callback != null )
			{
				noFrameDataCallbacks.Add( callback );
			}
		}
		
		public void UnregisterNoFrameDataCallback( NoFrameDataDelegate callback )
		{
			if( callback != null )
			{
				noFrameDataCallbacks.Remove( callback );
			}
		}
		
		public void RegisterResumeFrameDataCallback( ResumeFrameDataDelegate callback )
		{
			if( callback != null )
			{
				resumeFrameDataCallbacks.Add( callback );
			}
		}
		
		public void UnregisterResumeFrameDataCallback( ResumeFrameDataDelegate callback )
		{
			if( callback != null )
			{
				resumeFrameDataCallbacks.Remove( callback );
			}
		}
		
		public NeuronActor( NeuronSource owner, int actorID )
		{
			this.owner = owner;
			this.actorID = actorID;
			
			if( owner != null )
			{
				owner.RegisterResumeActorCallback( OnResumeFrameData );
				owner.RegisterSuspendActorCallback( OnNoFrameData );
				bvhDataRecord = new List<KeyValuePair<int, float[]>> ();
				calcDataRecord = new List<KeyValuePair<int, float[]>> ();
				recKvp = new List<KeyValuePair<int, float[]>> ();
				tagRecord = new List<StampTag> ();
				readTagKvp = new List<StampTag> ();
				calcRecStamp = new List<int> ();
				user = GameObject.FindGameObjectWithTag ("DataManeger").GetComponent<UserData> ();
			}
		}
		
		~NeuronActor()
		{
			if( owner != null )
			{
				owner.UnregisterResumeActorCallback( OnResumeFrameData );
				owner.UnregisterSuspendActorCallback( OnNoFrameData );
			}
		}

		// Record Motion
        
		public void StartRecord()
        {
            bvhDataRecord.Clear();
            calcDataRecord.Clear();
			tagRecord.Clear ();
            isRecording = true;
        }

        public void StopRecord()
        {
            isRecording = false;
        }

        public void StopRecordAndSave(string[] filePath)
        {
            if (isRecording)
            {
                StopRecord();

                // save data below
				isReading = true;
				// bvh
				FileStream fs_bvh = new FileStream (filePath [0], FileMode.Create);
				StreamWriter sw = new StreamWriter (fs_bvh);

				foreach (KeyValuePair<int, float[]> kvp in bvhDataRecord) {
					System.Text.StringBuilder line = new System.Text.StringBuilder();
					line.Append(kvp.Key + " ");

					for (int i = 0; i < kvp.Value.Length; i++) {
						line.Append(kvp.Value [i] + ",");
					}
					line.AppendLine();
					sw.Write (line.ToString());
				}
				sw.Flush ();
				sw.Close ();

				// calc
				FileStream fs_calc = new FileStream (filePath [1], FileMode.Create);
				sw = new StreamWriter (fs_calc);
				foreach (KeyValuePair<int, float[]> kvp in calcDataRecord) {
					System.Text.StringBuilder line = new System.Text.StringBuilder();
					line.Append(kvp.Key + " ");

					for (int i = 0; i < kvp.Value.Length; i++) {
						line.Append(kvp.Value [i] + ",");
					}
					line.AppendLine();
					sw.Write (line.ToString());
				}
				sw.Flush ();
				sw.Close ();

				// tag
				FileStream fs = new FileStream (filePath [2], FileMode.Create);
				sw = new StreamWriter (fs);
				for(int i=0; i<tagRecord.Count; i++) {
					System.Text.StringBuilder line = new System.Text.StringBuilder();
					if (tagRecord[i].endStamp != -1) {
						line.Append(i + " ");
						line.Append(tagRecord[i].name + " ");
						line.Append(tagRecord[i].startStamp + " ");
						line.Append(tagRecord[i].endStamp);
						line.AppendLine();
						sw.Write (line.ToString());
					}
				}
				sw.Flush ();
				sw.Close ();
				isReading = false;
				AssetDatabase.Refresh ();
			}
        }

        public bool IsRecording()
        {
            return isRecording;
        }

		// Playback Motion

		public void ReadPlaybackData(string filePath)
		{
			isReading = true;

			recKvp.Clear ();
			FileStream bvhFile = new FileStream (filePath, FileMode.Open, FileAccess.Read);
			StreamReader reader = new StreamReader (bvhFile,System.Text.Encoding.UTF8);
			string line = null;
			char[] separator = { ' ', ',' };
			line = reader.ReadLine ();
			while (line != null) {
				string[] s = line.Split (separator, line.Length);
				int timeStamp = Convert.ToInt32 (s[0]);
				float[] data = new float[MaxFrameDataLength];
				for (int i = 0; i < bvhHeader.DataCount; i++) {
					data [i] = Convert.ToSingle (s[i + 1]);
				}
				recKvp.Add(new KeyValuePair<int, float[]>(timeStamp, data));
				line = reader.ReadLine ();
			}
			bvhFile.Close ();
			reader.Close ();
			isReading = false;
			Debug.Log ("Finish bvh Reading.");
		}

		public void StartPlayback()
		{
			isPlayback = true;
			isPause = false;
		}

		public void StartPause()
		{
			isPause = true;
		}

		public void StopPlayback()
		{
			isPlayback = false;
			playCount = 0;
		}

		public void Playback()
		{
			if (isPlayback) {
				if (playCount < recKvp.Count) {
					playbackTimeStamp = recKvp [playCount].Key;
					for (int i = 0; i < recKvp [playCount].Value.Length; i++)
						recData [i] = recKvp [playCount].Value [i];
					if(!isPause)
						playCount++;
				} else
					StopPlayback ();
			}
		}

		public void setPlayCount(float value)
		{
			playCount = (int)(value * (recKvp.Count - 1));
		}

		public void forward() {
			if (isPause && playCount+1 < recKvp.Count) {
				playCount++;
			}
		}

		public void backward()
		{
			if (isPause && playCount - 1 > 0)
				playCount--;
		}

		public bool IsPlayback() { return isPlayback; }
		public bool IsPause()	 { return isPause; }


		// Received MotionData

		public void OnReceivedMotionData( BvhDataHeader header, IntPtr data )
		{
			this.bvhHeader = header;
			try
			{
                float[] bvhData = new float[header.DataCount];
				Marshal.Copy( data, bvhData, 0, (int)header.DataCount );
				bvhtimeStamp = GetTimeStamp();
                this.data = bvhData;
                if (isRecording)
                    bvhDataRecord.Add(new KeyValuePair<int, float[]>(bvhtimeStamp, bvhData));
			}
			catch( Exception e )
			{
				Debug.LogException( e );
			}
		}

        public void OnReceivedCalculationData(CalcDataHeader header, IntPtr data)
        {
            this.calcHeader = header;
            if (isRecording)
            {
                try
                {
                    float[] calcData = new float[header.DataCount];
                    Marshal.Copy(data, calcData, 0, (int)header.DataCount);
                    calctimeStamp = GetTimeStamp();
                    calcDataRecord.Add(new KeyValuePair<int, float[]>(calctimeStamp, calcData));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

		// Set Tag

		public void setStartStamp(string tagName)
		{
			tag = new StampTag (tagName, GetTimeStamp());
			tagRecord.Add (tag);
		}

		public void setEndStamp()
		{
			StampTag tag = tagRecord [tagRecord.Count - 1];
			tag.endStamp = GetTimeStamp();
			tagRecord.Insert (tagRecord.Count - 1, tag);
			tagRecord.RemoveAt (tagRecord.Count - 1);
		}

		public void ReadPlaybackTag(string filePath)
		{
			isReading = true;

			readTagKvp.Clear ();
			FileStream tagFile = new FileStream (filePath, FileMode.Open, FileAccess.Read);
			StreamReader reader = new StreamReader (tagFile,System.Text.Encoding.UTF8);
			string line = null;
			char[] separator = { ' ' };
			line = reader.ReadLine ();
			while (line != null) {
				string[] s = line.Split (separator, line.Length);
//				int index = Convert.ToInt32 (s[0]);
				StampTag tag = new StampTag ("", -1);
				tag.name = s[1];
				tag.startStamp = Convert.ToInt32 (s[2]);
				tag.endStamp = Convert.ToInt32 (s[3]);
				readTagKvp.Add(tag);
				line = reader.ReadLine ();
			}
			tagFile.Close ();
			reader.Close ();
			isReading = false;
			Debug.Log ("Finish Tag Reading.");
		}

        public virtual void OnNoFrameData( NeuronActor actor )
		{
            StopRecord();
			for( int i = 0; i < noFrameDataCallbacks.Count; ++i )
			{
				noFrameDataCallbacks[i]();
			}
		}		
		
		public virtual void OnResumeFrameData( NeuronActor actor  )
		{
			for( int i = 0; i < resumeFrameDataCallbacks.Count; ++i )
			{
				resumeFrameDataCallbacks[i]();
			}
		}
		
		public float[] GetData()
		{
			return data;
		}
		
		public BvhDataHeader GetHeader()
		{
			return bvhHeader;
		}
		
		public static int GetTimeStamp()
		{
			return DateTime.Now.Hour * 3600 * 1000 + DateTime.Now.Minute * 60 * 1000 + DateTime.Now.Second * 1000 + DateTime.Now.Millisecond;
		}

		public Vector3 GetReceivedPosition( NeuronBones bone )
		{
			int offset = 0;
			if( bvhHeader.bWithReference != 0 )
			{
				// skip reference
				offset += 6;
			}
			
			// got bone position data only when displacement is open or the bone is hips
			if( bvhHeader.bWithDisp != 0 || bone == NeuronBones.Hips )
			{
				// Hips position + Hips rotation + 58 * ( position + rotation )
				offset += (int)bone * 6;
				return new Vector3( -data[offset] * NeuronUnityLinearScale, data[offset+1] * NeuronUnityLinearScale, data[offset+2] * NeuronUnityLinearScale );
			}
			
			return Vector3.zero;
		}
		
		public Vector3 GetReceivedRotation( NeuronBones bone )
		{
			int offset = 0;
			if( bvhHeader.bWithReference != 0 )
			{
				// skip reference
				offset += 6;
			}
			
			if( bvhHeader.bWithDisp != 0 )
			{
				// Hips position + Hips rotation + 58 * ( position + rotation )
				offset += 3 + (int)bone * 6;
			}
			else
			{
				// Hips position + Hips rotation + 58 * rotation
				offset += 3 + (int)bone * 3;
			}
			
			return new Vector3( data[offset+1], -data[offset], -data[offset+2] );
		}

		public Vector3 GetRecordedPosition( NeuronBones bone )
		{
			int offset = 0;
			if( bvhHeader.bWithReference != 0 )
			{
				// skip reference
				offset += 6;
			}

			// got bone position data only when displacement is open or the bone is hips
			if( bvhHeader.bWithDisp != 0 || bone == NeuronBones.Hips )
			{
				// Hips position + Hips rotation + 58 * ( position + rotation )
				offset += (int)bone * 6;
				return new Vector3( -recData[offset] * NeuronUnityLinearScale, recData[offset+1] * NeuronUnityLinearScale, recData[offset+2] * NeuronUnityLinearScale );
			}

			return Vector3.zero;
		}

		public Vector3 GetRecordedRotation( NeuronBones bone )
		{
			int offset = 0;
			if( bvhHeader.bWithReference != 0 )
			{
				// skip reference
				offset += 6;
			}

			if( bvhHeader.bWithDisp != 0 )
			{
				// Hips position + Hips rotation + 58 * ( position + rotation )
				offset += 3 + (int)bone * 6;
			}
			else
			{
				// Hips position + Hips rotation + 58 * rotation
				offset += 3 + (int)bone * 3;
			}

			return new Vector3( recData[offset+1], -recData[offset], -recData[offset+2] );
		}
	}
}