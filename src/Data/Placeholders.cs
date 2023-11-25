using OpenBveApi.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace obDRPC {

    public class Placeholder {
        public string VariableName;
        public string Description;
        public Context Context;

        public Placeholder(string varName, string description, Context context) {
            this.VariableName = varName;
            this.Description = description;
            this.Context = context;
        }
    }

    public class Placeholders {
        public List<Placeholder> placeholderList = new List<Placeholder>();

        public Placeholders() {
            placeholderList.Add(new Placeholder("time", "Current In-Game time", Context.InGame));
            placeholderList.Add(new Placeholder("speedKmh", "Player's train speed in km/h", Context.InGame));
            placeholderList.Add(new Placeholder("speedMph", "Player's train speed in mph", Context.InGame));
            placeholderList.Add(new Placeholder("reverser", "Player's train reverser position", Context.InGame));
            placeholderList.Add(new Placeholder("powerNotch", "Player's train power notch", Context.InGame));
            placeholderList.Add(new Placeholder("brakeNotch", "Player's train brake notch", Context.InGame));
            placeholderList.Add(new Placeholder("notch", "Player's train notch (Combined)", Context.InGame));
            placeholderList.Add(new Placeholder("gradient", "Steepness of the slope (Driver-Car)", Context.InGame));
			placeholderList.Add(new Placeholder("prevStnName", "Name of previous station", Context.InGame));
			placeholderList.Add(new Placeholder("curStnName", "Name of current station", Context.Boarding));
			placeholderList.Add(new Placeholder("curStnDwell", "Dwell time of current station (sec)", Context.Boarding));
			placeholderList.Add(new Placeholder("curStnDwellLeft", "Dwell time left of current station (sec)", Context.Boarding));
			placeholderList.Add(new Placeholder("doors", "Door Direction", Context.Boarding));
			placeholderList.Add(new Placeholder("nextStnName", "Name of next station", Context.InGame));
            placeholderList.Add(new Placeholder("nextStnDist", "Distance of next station (m)", Context.InGame));
            placeholderList.Add(new Placeholder("programVersion", "OpenBVE Version", Context.Menu));
        }

        public string ParsePlaceholders(string input, string progVersion, ElapseData data, VehicleSpecs specs, DoorStates doorState, Context context, int charLimit) {
            if (string.IsNullOrEmpty(input)) return "";

			StringBuilder sb = new StringBuilder(input);

			foreach (Placeholder placeholder in placeholderList) {
				if (placeholder.Context != Context.Menu && data == null) continue;
				if (placeholder.Context == Context.Boarding && context != Context.Boarding) continue;
				if (placeholder.Context != Context.Menu && context == Context.Menu) continue;

				switch (placeholder.VariableName) {
					case "time":
						sb.Replace("{time}", DateTimeOffset.FromUnixTimeMilliseconds((long)data.TotalTime.Milliseconds).ToString("HH:mm:ss"));
						continue;
					case "speedKmh":
						sb.Replace("{speedKmh}", Math.Abs(data.Vehicle.Speed.KilometersPerHour).ToString("0"));
						continue;
					case "speedMph":
						sb.Replace("{speedKmh}", Math.Abs(data.Vehicle.Speed.MilesPerHour).ToString("0"));
						continue;
					case "reverser":
						sb.Replace("{reverser}", data.Handles.Reverser == -1 ? "B" : data.Handles.Reverser == 0 ? "N" : "F");
						continue;
					case "powerNotch":
						sb.Replace("{powerNotch}", data.Handles.PowerNotch > 0 ? "P" + data.Handles.PowerNotch : "N");
						continue;
					case "brakeNotch":
						sb.Replace("{brakeNotch}", data.Handles.BrakeNotch == specs.BrakeNotches + 1 ? "EMG" : data.Handles.BrakeNotch > 0 ? "B" + data.Handles.BrakeNotch : "N");
						continue;
					case "notch":
						sb.Replace("{notch}", data.Handles.BrakeNotch > 0 ? (data.Handles.BrakeNotch == specs.BrakeNotches + 1 ? "EMG" : data.Handles.BrakeNotch > 0 ? "B" + data.Handles.BrakeNotch : "N") : data.Handles.PowerNotch > 0 ? "P" + data.Handles.PowerNotch : "N");
						continue;
					case "gradient":
						sb.Replace("{gradient}", Math.Abs(data.Vehicle.Pitch / 100).ToString("0.##") + "%" + (data.Vehicle.Pitch > 0 ? "↗" : data.Vehicle.Pitch < 0 ? "↘" : ""));
						continue;
					case "nextStnName":
						sb.Replace("{nextStnName}", StationManager.NextStation == null ? "" : StationManager.NextStation.Name);
						continue;
					case "prevStnName":
						sb.Replace("{prevStnName}", StationManager.PreviousStation == null ? "" : StationManager.PreviousStation.Name);
						continue;
					case "nextStnDist":
						sb.Replace("{nextStnDist}", Math.Abs(StationManager.NextStnDist) < 1 ? StationManager.NextStnDist.ToString("F") : ((int)Math.Round(StationManager.NextStnDist)).ToString());
						continue;
					case "doors":
						sb.Replace("{doors}", doorState == DoorStates.Left ? "<<< Doors" : doorState == DoorStates.Right ? "Doors >>>" : "<<< Doors >>>");
						continue;
					case "curStnName":
						sb.Replace("{curStnName}", StationManager.CurrentStation.Name);
						continue;
					case "curStnDwell":
						sb.Replace("{curStnDwell}", StationManager.CurrentStation.StopTime.ToString());
						continue;
					case "curStnDwellLeft":
						sb.Replace("{curStnDwellLeft}", ((int)StationManager.DwellLeft).ToString());
						continue;
					case "programVersion":
						sb.Replace("{programVersion}", progVersion);
						continue;
					default:
						continue;
				}
			}

			return sb.ToString(0, Math.Min(sb.Length, charLimit));
		}
    }
}
