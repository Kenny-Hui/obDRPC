using OpenBveApi.Runtime;

namespace obDRPC {
    /// <summary>
    /// Informations Default Displaying Plugin (for Testing)
    /// </summary>
    public static class StationManager {

		private const int STOP_TOLERANCE = 5;
		private static double DepTime;
		public static Station NextStation;
		public static Station CurrentStation;
		public static Station PreviousStation;
		public static double NextStnDist;
		public static bool Boarding;
		public static double DwellLeft;

		public static void Update(ElapseData data, DoorStates doorState) {
			double trainPosition = data.Vehicle.Location;
			bool trainLeftDoorOpened = doorState == DoorStates.Left || doorState == DoorStates.Both;
			bool trainRightDoorOpened = doorState == DoorStates.Right || doorState == DoorStates.Both;

			for (int i = 0; i < data.Stations.Count; i++) {
				Station stn = data.Stations[i];
				if (stn.StopMode == StationStopMode.PlayerPass || stn.StopMode == StationStopMode.AllPass) continue;
				if (stn.StopPosition + STOP_TOLERANCE <= trainPosition) continue;
				NextStation = stn;
				NextStnDist = NextStation.StopPosition - trainPosition;
				PreviousStation = i == 0 ? data.Stations[0] : data.Stations[i - 1];

				if (trainPosition > NextStation.DefaultTrackPosition && trainPosition < NextStation.StopPosition + STOP_TOLERANCE && doorState != DoorStates.None) {
					bool doorOpenedCorrectly = (trainLeftDoorOpened && stn.OpenLeftDoors) || (trainRightDoorOpened && stn.OpenRightDoors) || ((trainLeftDoorOpened && trainRightDoorOpened) && (stn.OpenLeftDoors && stn.OpenRightDoors));
					// Assume we are boarding
					if (doorOpenedCorrectly) {
						obDRPC.CurrentContext = Context.Boarding;
						CurrentStation = stn;
						Boarding = true;
						if (DepTime == 0) {
                            // If still have more time than minimum stop time, use departure time.
							if (stn.DepartureTime - data.TotalTime.Seconds > stn.StopTime) {
								DepTime = stn.DepartureTime;
							} else {
								DepTime = data.TotalTime.Seconds + stn.StopTime;
							}
						}
						DwellLeft = DepTime - data.TotalTime.Seconds;
					}
				} else {
					CurrentStation = null;
					Boarding = false;
					DepTime = 0;
					obDRPC.CurrentContext = Context.InGame;
				}
				break;
			}
		}
	}
}
