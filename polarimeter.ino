//#include <MsTimer2.h>
int led = 13;
int pwm_pin = 6;
 
int j_center = 127;
int range = 127;

int mode = -1;
const int MODE_SCAN = 1;
const int MODE_MAIN = 0;
const int MODE_IDLE = -1;

String serialString = "aaa";
String str;
String state = "";
int ac_value = 0;
int dc_value = 0;
int serialTimer = 0;

void setup() {
  Serial.begin(9600);
  pinMode(led, OUTPUT);
  pinMode(pwm_pin, OUTPUT);
  mode = MODE_IDLE;
  //MsTimer2::set(100, timerInterrupt);
  //MsTimer2::start();
}

//void checkSerial(){
//  if (Serial.available() > 0)
//  {
//     char incomingChar = Serial.read();  
//      if (incomingChar == 's'){
//        mode = MODE_SCAN;
//      } else if (incomingChar == 'm'){
//        mode = MODE_MAIN;
//      } else if (incomingChar == 'i'){
//        mode = MODE_IDLE;
//      }
//  }
//}

void checkSerial(){
  str = getString();
  if (str != ""){
      state = getValue(str, ' ', 0);
      ac_value = getValue(str, ' ', 1).toInt();
      dc_value = getValue(str, ' ', 2).toInt();
      //Serial.print(state);
  }
  if (state == "s") mode = MODE_SCAN;
  if (state == "m") mode = MODE_MAIN;
  if (state == "i") mode = MODE_IDLE;
  state = "";
  //Serial.print(serialString);
}

String getString(){
  String tempStr = "";
  while (Serial.available() > 0)
  {
    char incomingChar = Serial.read();
    if (incomingChar == '\n') {
      return tempStr;
    }
    tempStr += incomingChar;
  }
}

String getValue(String data, char separator, int index)
{
  int found = 0;
  int strIndex[] = {0, -1};
  int maxIndex = data.length()-1;

  for(int i=0; i<=maxIndex && found<=index; i++){
    if(data.charAt(i)==separator || i==maxIndex){
        found++;
        strIndex[0] = strIndex[1]+1;
        strIndex[1] = (i == maxIndex) ? i+1 : i;
    }
  }

  return found>index ? data.substring(strIndex[0], strIndex[1]) : "";
}
bool main_flag = 0;
int pwm_value=0; //pwm voltage
void mode_main(){
    //Serial.print("This is main\n");
    digitalWrite(led, LOW);
    serialString = "m " + String(pwm_value) + "\n";
    analogWrite(pwm_pin, pwm_value);
       
    if (pwm_value == j_center + range) main_flag = 0;
    if (pwm_value == (j_center - range) or pwm_value == 0) main_flag = 1;
    if (main_flag == 0) pwm_value--;
    else pwm_value++;
}

//void mode_idle(){
//  serialString = str + '\n';
//  Serial.print("This is idle\n");
//}

int scan_step=0;
void mode_scan(){
  //Serial.print("this is Scan\n");
    digitalWrite(led, HIGH);
    if (scan_step<510){
      pwm_value = 255-abs(scan_step-255);
      serialString = "s " + String(pwm_value) + "\n";
      analogWrite(pwm_pin, pwm_value);
      scan_step++;
      delay(100);  
    }
    if (scan_step==510) {
      mode = MODE_MAIN;
      scan_step = 0;
    }
}

void mode_idle(){
  digitalWrite(led, LOW);
  analogWrite(pwm_pin, 0);
  serialString = "i\n";
}

void timerInterrupt(){
  if (mode != MODE_IDLE) Serial.print(serialString);
  checkSerial();
}

void loop() {
  if (mode == MODE_MAIN) mode_main();
  if (mode == MODE_SCAN) mode_scan();
  if (mode == MODE_IDLE) mode_idle();
  if (mode != MODE_SCAN) scan_step = 0;
  if (millis()-serialTimer >=100){
    if (mode!= MODE_IDLE) Serial.print(serialString);
    checkSerial();
    serialTimer = millis();    
  }
}
