using Microsoft.AspNetCore.SignalR;
using SmokeMaster.Shared;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace SmokeMaster.Server.Hubs
{
  public class UiHub : Hub<ISmokerClient>
  {
    private readonly SmokerDataService mSmokerDataService;

    public UiHub(SmokerDataService smokerDataService)
    {
      mSmokerDataService = smokerDataService;
    }

    public override async Task OnConnectedAsync()
    {
      await Clients.Caller.UpdateUiData(mSmokerDataService.UiData);
      await base.OnConnectedAsync();
    }

    public async Task Example(int fanSpeed)
    {
      await mSmokerDataService.SetFanSpeed(Convert.ToUInt32(fanSpeed));
    }
  }

  public interface ISmokerClient
  {
    Task UpdateUiData(UiData data);
  }

  [StructLayout(LayoutKind.Sequential)]
  internal struct PigsCommand
  {
    public uint mCommand;
    public uint mParam1;
    public uint mParam2;
    public uint mParam3;

    public byte[] ToByteArray()
    {
      byte[] wBuffer = new byte[Marshal.SizeOf(typeof(PigsCommand))];
      GCHandle wHandle = GCHandle.Alloc(wBuffer, GCHandleType.Pinned);
      Marshal.StructureToPtr(this, wHandle.AddrOfPinnedObject(), false);
      wHandle.Free();
      return wBuffer;
    }
  }

  public class SmokerDataService : BackgroundService
  {
    private readonly ILogger<SmokerDataService> mLogger;
    private readonly IHubContext<UiHub, ISmokerClient> mUiHub;
    private readonly UdpClient mTemperatureClient = new();
    private readonly TcpClient mPigsControl = new();

    public UiData UiData { get; set; } = new UiData();

    public SmokerDataService(ILogger<SmokerDataService> logger, IHubContext<UiHub, ISmokerClient> uiHub)
    {
      mLogger = logger;
      mUiHub = uiHub;
      mTemperatureClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
      mTemperatureClient.Client.Bind(new IPEndPoint(IPAddress.Any, 14587));
      mTemperatureClient.JoinMulticastGroup(IPAddress.Parse("224.0.0.1"));
      mPigsControl.Connect(new IPEndPoint(IPAddress.Parse("192.168.3.241"), 8888));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
      while (true)
      {
        var wFrame = await mTemperatureClient.ReceiveAsync(stoppingToken);
        UiData.InnerTemperature = Convert.ToDouble(System.Text.Encoding.UTF8.GetString(wFrame.Buffer));
        await mUiHub.Clients.All.UpdateUiData(UiData);
      }
    }

    public async Task SetFanSpeed(uint fanSpeed)
    {
      var wCommand = new PigsCommand() { mCommand = 5, mParam1 = 12, mParam2 = fanSpeed, mParam3 = 0 };
      await mPigsControl.GetStream().WriteAsync(wCommand.ToByteArray());
      await mPigsControl.GetStream().ReadAsync(new byte[Marshal.SizeOf(typeof(PigsCommand))]);
    }
  }
}
