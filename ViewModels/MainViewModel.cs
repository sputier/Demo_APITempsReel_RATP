using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HorairesRatp.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private IEnumerable<ServiceRatp.Line> _lines;
        public IEnumerable<ServiceRatp.Line> Lines
        {
            get { return _lines; }
            set
            {
                _lines = value;
                RaisePropertyChanged();
            }
        }

        private ServiceRatp.Line _selectedLine;
        public ServiceRatp.Line SelectedLine
        {
            get { return _selectedLine; }
            set
            {
                _selectedLine = value;
                RaisePropertyChanged();
            }
        }

        private IEnumerable<ServiceRatp.Direction> _lineDirections;
        public IEnumerable<ServiceRatp.Direction> LineDirections
        {
            get { return _lineDirections; }
            set
            {
                _lineDirections = value;
                RaisePropertyChanged();
            }
        }

        private ServiceRatp.Direction _selectedLineDirection;
        public ServiceRatp.Direction SelectedLineDirection
        {
            get { return _selectedLineDirection; }
            set
            {
                _selectedLineDirection = value;
                RaisePropertyChanged();
            }
        }

        private IEnumerable<ServiceRatp.Station> _lineStations;
        public IEnumerable<ServiceRatp.Station> LineStations
        {
            get { return _lineStations; }
            set
            {
                _lineStations = value;
                RaisePropertyChanged();
            }
        }

        private ServiceRatp.Station _selectedLineStation;
        public ServiceRatp.Station SelectedLineStation
        {
            get { return _selectedLineStation; }
            set
            {
                _selectedLineStation = value;
                RaisePropertyChanged();
            }
        }

        private string _apiVersion;
        public string ApiVersion
        {
            get { return _apiVersion; }
            set
            {
                _apiVersion = value;
                RaisePropertyChanged();
            }
        }

        private string _nextTrainWaitTime;
        public string NextTrainWaitTime
        {
            get { return _nextTrainWaitTime; }
            set
            {
                _nextTrainWaitTime = value;
                RaisePropertyChanged();
            }
        }

        public RelayCommand InitializeCommand { get; set; }
        public RelayCommand SelectedLineChangedCommand { get; set; }
        public RelayCommand SelectedDirectionChangedCommand { get; set; }
        public RelayCommand SelectedStationChangedCommand { get; set; }


        public MainViewModel()
        {
            InitializeCommand = new RelayCommand(InitializeAction);
            SelectedLineChangedCommand = new RelayCommand(SelectedLineChangedAction);
            SelectedDirectionChangedCommand = new RelayCommand(SelectedDirectionChangedAction);
            SelectedStationChangedCommand = new RelayCommand(SelectedStationChangedAction);
        }

        public async void InitializeAction()
        {
            var service = new ServiceRatp.WsivPortTypeClient();

            var version = await service.getVersionAsync();
            ApiVersion = version.@return;

            //liste des lignes de métro
            var lignesMetro = await service.getLinesAsync(new ServiceRatp.Line() { reseau = new ServiceRatp.Reseau() { code = "metro" } });
            //liste des lignes RER
            var lignesRER = await service.getLinesAsync(new ServiceRatp.Line() { reseau = new ServiceRatp.Reseau() { code = "rer" } });

            var allLines = lignesMetro.@return.Where(l => l.code[0] >= '1' && l.code[0] <= '9')
                                                .Concat(lignesRER.@return.Where(l => l.codeStif != "0"))
                                                .ToList();
            allLines.ForEach(l => l.image = GetLineImagePath(l));

            Lines = allLines;
        }

        public async void SelectedLineChangedAction()
        {
            var service = new ServiceRatp.WsivPortTypeClient();

            var directions = await service.getDirectionsAsync(_selectedLine);
            LineDirections = directions.@return.directions;
        }

        private string GetLineImagePath(ServiceRatp.Line line)
        {
            if (line.reseau.code == "metro")
                return "M_" + line.code + (line.code.EndsWith("b") ? "is" : "") + ".png";
            else if (line.reseau.code == "rer")
                return "RER_" + line.code + ".png";

            return null;
        }

        private async void SelectedDirectionChangedAction()
        {
            string temp = null;
            if (SelectedLineStation != null)
                temp = SelectedLineStation.name;

            var service = new ServiceRatp.WsivPortTypeClient();

            var stations = await service.getStationsAsync(new ServiceRatp.Station() { line = _selectedLine, direction = _selectedLineDirection }, null, null, 100, false);
            LineStations = stations.@return.stations;

            if (temp != null)
                this.SelectedLineStation = LineStations.FirstOrDefault(station => station.name == temp);
        }

        private async void SelectedStationChangedAction()
        {
            if (SelectedLineStation == null)
            {
                NextTrainWaitTime = null;
                return;
            }

            var service = new ServiceRatp.WsivPortTypeClient();

            var nextMission = await service.getMissionsNextAsync(_selectedLineStation, _selectedLineDirection, null/*DateTime.Now.ToString("yyyyMMddHHmm")*/, 1);

            string result = null;
            var dateString = nextMission.@return.missions.FirstOrDefault()?.stationsDates?[0];

            if (dateString == null)
                return;

            var date = new DateTime(int.Parse(dateString.Substring(0, 4)),
                                    int.Parse(dateString.Substring(4, 2)),
                                    int.Parse(dateString.Substring(6, 2)),
                                    int.Parse(dateString.Substring(8, 2)),
                                    int.Parse(dateString.Substring(10, 2)),
                                    0);

            var waitTime = date - DateTime.Now;

            if (waitTime.TotalHours < 1)
                result = string.Format("{0:00} min", (int)waitTime.TotalMinutes);
            else
                result = string.Format("+{0} h", (int)waitTime.TotalHours);

            NextTrainWaitTime = result;
        }
    }

}
