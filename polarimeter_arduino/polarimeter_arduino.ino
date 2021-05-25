//#include <MsTimer2.h>
#include <avr/wdt.h> // This is watchdog, timer is set to 8s with wdt_enable in setup(), and is reset with wdt_reset in loop(). So, if system stucks, wdt_reset() will not be called and system will hard reboot.
//WARNING! This code works only for Arduino UNO! Other boards i.e. Mega, Nano etc. use different bootloader and this watchdog won't work correctly.

//PWM with changeable duty cycle will be on the pin 9!!! It is 10 bit (acceptable values 0-1023) and has frequency ~31kHz
//PWM with 50% duty cycle will be on the pin 6 with 980Hz freq.
int led = 13;

int voltage_center = 511;
int voltage_range = 200;

int mode = -1;
const int MODE_SCAN = 1;
const int MODE_MAIN = 0;
const int MODE_IDLE = -1;

String serialString = "";
unsigned long serialTimer = 0;
unsigned long micros_delay = 0; // timing of constant pwm_value for mod_main, is calculated as (1000000 / mod_freq) / (2 * voltage_range);
int mod_freq = 10; // frequency of cell modulation in Hz, is used to calculate micros_delay;
unsigned long current_micros = 0;
unsigned long scan_delay = 100; //timing of one step of scan in milliseconds, default value is 100
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
  String str = getString();
  String state = "";
  int temp_int_1 = 0;
  int temp_int_2 = 0;
  if (str != "") {
    state = getValue(str, ' ', 0);
    temp_int_1 = getValue(str, ' ', 1).toInt();
    temp_int_2 = getValue(str, ' ', 2).toInt();
    //Serial.print(state);
  }
  if (temp_int_1 != 0) voltage_center = temp_int_1;
  if (temp_int_2 != 0) voltage_range = temp_int_2;
  if (state == "s") mode = MODE_SCAN;
  if (state == "m") mode = MODE_MAIN;
  if (state == "i") mode = MODE_IDLE;
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
    if (pwm_value >= voltage_center + voltage_range / 2) main_flag = 0;
    if (pwm_value <= (voltage_center - voltage_range / 2)) main_flag = 1;
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
  if ((scan_step < 2046) and (millis() - step_timer > scan_delay)) {
    pwm_value = 1023 - abs(scan_step - 1023);
    serialString = "s " + String(pwm_value) + "\n";
    PWMWrite(pwm_value);
    scan_step++;
    step_timer = millis();
  }
  if (scan_step == 2046) {
    mode = MODE_MAIN;
    scan_step = 0;
  }
}

void mode_idle() {
  digitalWrite(led, LOW);
  PWMWrite(0);
  //analogWrite(9, 0);
  analogWrite(6, 0);
  serialString = "i\n";
}

void PWMSetup() {
  pinMode(9, OUTPUT);
  //Timer works with ~31kHz freq, last three bits of TCCR1B set the prescaler for frequency: 001 - 1, 010 - 8, 011 - 64, 100 - 256, 101 - 1024;
  // PWM mode is controlled with bits 4 and 3 of TCCR1B and with bits 1 and 0 of TCCR1A, here mode 7 (0111, Fast 10 bit PWM) is used.
  TCCR1B = TCCR1B & B11100000 | B00001001;
  TCCR1A = (TCCR1A & B00111100) | B10000011;
}

void PWMWrite(int val) {
  analogWrite(9, val);
}

void setup() {
  Serial.begin(9600);
  pinMode(led, OUTPUT);
  PWMSetup();
  mode = MODE_IDLE;
  current_micros = micros();
  //MsTimer2::set(100, timerInterrupt);
  //MsTimer2::start();
  wdt_enable(WDTO_1S); // Possible values:
  /* Возможные значения для константы
  WDTO_15MS
  WDTO_30MS
  WDTO_60MS
  WDTO_120MS
  WDTO_250MS
  WDTO_500MS
  WDTO_1S
  WDTO_2S
  WDTO_4S
  WDTO_8S
*/
}

void loop() {
  if (mode != MODE_SCAN) scan_step = 0;
  if (millis() - serialTimer > 100) {
    if (mode != MODE_IDLE) {
      //Serial.print(millis() - serialTimer);
      Serial.print(serialString);
      micros_delay = (1000000 / mod_freq) / (2 * voltage_range);
      analogWrite(6, 127);
      wdt_reset();
    }
    checkSerial();
    serialTimer = millis();
  }
  if (mode == MODE_MAIN) mode_main();
  if (mode == MODE_SCAN) mode_scan();
  if (mode == MODE_IDLE) mode_idle();
  //analogWrite(10, 511); // meander for modulator
}
