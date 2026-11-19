public static class HomeBasePresentation {
 public const string Name="숙소";
 public const string ParkingName="기사 숙소 주차장";
 public const string RentLabel="숙소 월세";
 public const string EndNotice="숙소 주차 칸에 정차해야 퇴근할 수 있습니다. F7에서 숙소 길 안내를 받으세요.";

 public static bool IsFacility(MapData map,ProjectMapObject facility){
  return map.mapId=="eungye"&&facility!=null&&facility.kind=="parking"&&facility.id=="service:parking";
 }
}
