//#include <MsTimer2.h>
//PWM with changeable duty cycle will be on the pin 9!!! It is 10 bit (acceptable values 0-1023) and has frequency ~31kHz
//PWM with 50% duty cycle will be on the pin 10.
int led = 13;

int voltage_center = 511;
int voltage_range = 200;

int mode = -1;
const int MODE_SCAN = 1;
const int MODE_MAIN = 0;
const int MODE_IDLE = -1;

String serialString = "";
String str;
String state = "";
unsigned long serialTimer = 0;
unsigned long micros_delay = 0;
int mod_freq = 10; // frequency of cell modulation in Hz, is used to calculate micros_delay;
unsigned long current_micros = 0;
void setup() {
  Serial.begin(9600);
  pinMode(led, OUTPUT);
  PWMSetup();
  mode = MODE_IDLE;
  current_micros = micros();
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

void checkSerial() {
  str = getString();
  if (str != "") {
    state = getValue(str, ' ', 0);
    //voltage_center = getValue(str, ' ', 1).toInt();
    //voltage_range = getValue(str, ' ', 2).toInt();
    //Serial.print(state);
  }
  if (state == "s") mode = MODE_SCAN;
  if (state == "m") mode = MODE_MAIN;
  if (state == "i") mode = MODE_IDLE;
  state = "";
  //Serial.print(serialString);
}

String getString() {
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
  int maxIndex = data.length() - 1;

  for (int i = 0; i <= maxIndex && found <= index; i++) {
    if (data.charAt(i) == separator || i == maxIndex) {
      found++;
      strIndex[0] = strIndex[1] + 1;
      strIndex[1] = (i == maxIndex) ? i + 1 : i;
    }
  }

  return found > index ? data.substring(strIndex[0], strIndex[1]) : "";
}
bool main_flag = 0;
int pwm_value = 0; //pwm voltage
void mode_main() {
  //Serial.print("This is main\n");
  digitalWrite(led, LOW);
  serialString = "m " + String(pwm_value) + "\n";
  PWMWrite(pwm_value);
  if (micros() - current_micros > micros_delay) {
    current_micros = micros();
    if (pwm_value == voltage_center + voltage_range / 2) main_flag = 0;
    if (pwm_value == (voltage_center - voltage_range / 2) or pwm_value == 0) main_flag = 1;
    if (main_flag == 0) pwm_value--;
    else pwm_value++;
  }
}

//void mode_idle(){
//  serialString = str + '\n';
//  Serial.print("This is idle\n");
//}

int scan_step = 0;
unsigned long step_timer = 0;
void mode_scan() {
  //Serial.print("this is Scan\n");
  digitalWrite(led, HIGH);
  if ((scan_step < 510) and (millis() - step_timer > 100)) {
    pwm_value = 255 - abs(scan_step - 255);
    serialString = "s " + String(pwm_value) + "\n";
    PWMWrite(pwm_value);
    scan_step++;
    step_timer = millis();
  }
  if (scan_step == 510) {
    mode = MODE_MAIN;
    scan_step = 0;
  }
}

void mode_idle() {
  digitalWrite(led, LOW);
  PWMWrite(0);
  serialString = "i\n";
}

void loop() {
  if (mode != MODE_SCAN) scan_step = 0;
  if (voltage_range != 0) micros_delay = (1000000 / mod_freq) / (2 * voltage_range);
  if (millis() - serialTimer >= 100) {
    if (mode != MODE_IDLE) {
      //Serial.print(millis() - serialTimer);
      Serial.print(serialString);
    }
    checkSerial();
    serialTimer = millis();
  }
  if (mode == MODE_MAIN) mode_main();
  if (mode == MODE_SCAN) mode_scan();
  if (mode == MODE_IDLE) mode_idle();
  analogWrite(10, 511); // meander for modulator
}

void PWMSetup() {
  pinMode(9, OUTPUT);
  //Timer works with ~31kHz freq, last three bits of TCCR1B set the prescaler for frequency: 001 - 1, 010 - 8, 011 - 64, 100 - 256, 101 - 1024;
  // PWM mode is controlled with bits 4 and 3 of TCCR1B and with bits 1 and 0 of TCCR1A, here mode 7 (0111, Fast 10 bit PWM) is used.
  TCCR1B = TCCR1B & B11100000 | B00001001;
  TCCR1A = (TCCR1A & B00111100) | B10000011;
}

void PWMWrite(int val) {
  OCR1A = val;
}
