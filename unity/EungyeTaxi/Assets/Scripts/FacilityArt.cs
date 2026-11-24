using System.Collections.Generic;
using UnityEngine;

public static class FacilityArt {
 const string Directory="UI/Facilities/";
 static readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
 static readonly string[] backgrounds={"diner","fuel","wash","workshop","rest","parking","garage","room"};
 // kimbap/coffee (2026-11-24): drawn by tools/art/draw_refreshment_cards.py for the snack-shop cards.
 static readonly string[] meals={"pork","stew","soup","kimbap","coffee"};

 public static Texture2D Texture(string key){
  int index=System.Array.IndexOf(meals,key);
  bool meal=index>=0;
  var choices=meal?meals:backgrounds;
  if(!meal)index=System.Array.IndexOf(backgrounds,key);
  if(index<0)return null;
  key=choices[ContentTuning.Int((meal?"display.meal.":"display.background.")+key,index)];
  if(!textures.TryGetValue(key,out var image)){
   image=Resources.Load<Texture2D>(Directory+key);
   textures.Add(key,image);
   if(!image)Debug.LogError("Missing facility image: "+Directory+key);
  }
  return image;
 }
 public static string MealName(int index,string fallback){return ContentTuning.Text("display.menuName."+meals[index],fallback);}
 public static void Background(string key,Rect screen){
  var image=Texture(key);
  if(image)GUI.DrawTexture(screen,image,ScaleMode.ScaleAndCrop);
 }
 // One-item menu card for the kimbap shop and the cafe, in the diner card's look (always shown selected).
 public static void MenuCard(Rect card,string key,string name){
  CasualUI.RoundedFill(card,CasualUI.Blue);CasualUI.RoundedFill(new Rect(card.x+3,card.y+3,card.width-6,card.height-6),Color.white);
  var image=Texture(key);if(image)GUI.DrawTexture(new Rect(card.x+8,card.y+7,card.width-16,card.height-52),image,ScaleMode.ScaleToFit);
  var caption=new Rect(card.x+8,card.yMax-39,card.width-16,31);CasualUI.RoundedFill(caption,CasualUI.Soft);
  GUI.Label(caption,name,new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,fontSize=18,alignment=TextAnchor.MiddleCenter,normal={textColor=CasualUI.Ink}});
 }
 public static void MealCard(Rect card,int index,string name,ServiceStops services){
  bool selected=index==services.SelectedMeal;
  bool clicked=GUI.Button(card,new GUIContent("",name),GUIStyle.none);
  CasualUI.RoundedFill(card,selected?CasualUI.Blue:CasualUI.Border);
  CasualUI.RoundedFill(new Rect(card.x+3,card.y+3,card.width-6,card.height-6),Color.white);
  var image=Texture(meals[index]);
  if(image)GUI.DrawTexture(new Rect(card.x+8,card.y+7,card.width-16,card.height-52),image,ScaleMode.ScaleToFit);
  var caption=new Rect(card.x+8,card.yMax-39,card.width-16,31);
  CasualUI.RoundedFill(caption,CasualUI.Soft);
  GUI.Label(caption,name,new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,fontSize=18,alignment=TextAnchor.MiddleCenter,normal={textColor=CasualUI.Ink}});
  if(selected){
   var mark=new Rect(card.xMax-32,card.y+6,26,26);
   CasualUI.RoundedFill(mark,CasualUI.Blue);
   GUI.Label(mark,"✓",new GUIStyle(GUI.skin.label){font=KoreanText.BoldFont,fontSize=18,alignment=TextAnchor.MiddleCenter,normal={textColor=Color.white}});
  }
  if(clicked)services.SelectMeal(index);
 }
}
