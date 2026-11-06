import './public/passenger-presets.js';
export const PassengerPresets=globalThis.PassengerPresets;
export const PRESET_ENTRY={id:'passenger.presets',group:'승객 프리셋',section:'승객 프리셋',title:'승객 프리셋',type:'text',min:0,max:0,unit:'',defaultValue:JSON.stringify(PassengerPresets.defaults),description:'기존 행동을 조합하는 승객 프리셋. 공통값을 상속하며 승객별 대사·확률·효과를 개별 지정할 수 있습니다.',affects:['만족도','청결','시간'],modes:['single','pvp'],status:'implemented',consumer:'PassengerPresetCatalog',editable:true};
