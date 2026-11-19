public static class HomeBasePresentation {
 public const string Name="숙소";
 public const string ParkingName="기사 숙소 주차장";
 public const string EntryGuide=ParkingName+" · 덕성프라자 입구에서 H 해제 · 1층 주차 칸에 정차 후 F 퇴근";
 public const string ExitGuide="숙소 출구까지 직접 운전하세요 · 도로에 합류하면 길 안내가 이어집니다";
 public const string RentLabel="숙소 월세";
 public const string EndNotice="숙소 주차 칸에 정차해야 퇴근할 수 있습니다. F7에서 숙소 길 안내를 받으세요.";

 public static bool IsFacility(MapData map,ProjectMapObject facility){
  return map.mapId=="eungye"&&facility!=null&&facility.kind=="parking"&&facility.id=="service:parking";
 }
}
