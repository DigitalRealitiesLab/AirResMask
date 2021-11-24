#include <ESP32Servo.h>
#include "BluetoothSerial.h"
#include <Wire.h>

#define POLYNOMIAL 0x31 // Martin Wolf: P(x)=x^8+x^5+x^4+1 = 100110001

BluetoothSerial BT;

Servo SM;
int maxAngle = 180;
int OutputPin = 18;

uint8_t CRC_prim (uint8_t x, uint8_t crc)
{
  crc ^= x;
  for (uint8_t bit = 8; bit > 0; --bit)
  {
    if (crc & 0x80)
    {
      crc = (crc << 1) ^ POLYNOMIAL;
    }
    else
    {
      crc = (crc << 1);
    }
  }
  return crc;
}

#define sfm3300i2c 0x40

const unsigned mms = 20; // Martin Wolf: Measurement interval in ms

void SFM_init()
{
  // Martin Wolf: Soft reset
  Wire.beginTransmission(sfm3300i2c);
  Wire.write(0x20);
  Wire.write(0x00);
  Wire.endTransmission();
  delay(100);

  // Martin Wolf: Start continuous measurement
  Wire.beginTransmission(sfm3300i2c);
  Wire.write(0x10);
  Wire.write(0x00);
  Wire.endTransmission();
  delay(100);
  
  // Martin Wolf: Discard the first chunk of data that is always 0xFF
  Wire.requestFrom(sfm3300i2c,3);
  while (Wire.available())
  {
    Wire.read();
  }
  delay(mms);
 
  // Martin Wolf: Discard the first value
  Wire.requestFrom(sfm3300i2c,3);
  while (Wire.available())
  {
    Wire.read();
  }
  delay(mms);
}

void setup()
{
  ESP32PWM::allocateTimer(0);
  ESP32PWM::allocateTimer(1);
  ESP32PWM::allocateTimer(2);
  ESP32PWM::allocateTimer(3);
  SM.setPeriodHertz(50);
  SM.attach(OutputPin, 500, 2500);
  Wire.begin();
  Serial.begin(115200);
  delay(500); // Martin Wolf: Let serial console settle
  SFM_init();
  BT.begin("InhaleVR");
}

bool crc_error;

void SFM_measure()
{
  if (3 == Wire.requestFrom(sfm3300i2c, 3))
  {
    uint8_t crc = 0;
    uint8_t a = Wire.read();
    crc = CRC_prim (a, crc);
    uint8_t  b = Wire.read();
    crc = CRC_prim (b, crc);
    uint8_t  c = Wire.read();
    
    if (crc_error = (crc != c))
    {
      return;
    }
    
    uint8_t values[2];
    values[0] = a;
    values[1] = b;
    if (BT.available() == 0 && BT.hasClient())
    {
      BT.write(values, 2); // Martin Wolf: Send sensor data as a two byte integer value
    }
  }
  else
  {
    while (Wire.available())
    {
      Wire.read();
    }
    SFM_init();
  }
}

void ChangeAirResistance(int value)
{
  if (value > maxAngle)
  {
    SM.write(maxAngle);
  }
  else if (value < 0)
  {
    SM.write(0);
  }
  else
  {
    SM.write(value);
  }
}

unsigned long ms_prev = millis();

void loop()
{
  unsigned long ms_curr = millis();
  // Martin Wolf: Soft interrupt every mms milliseconds
  if (ms_curr - ms_prev >= mms)
  {
    ms_prev = ms_curr;
    SFM_measure();

    if (!BT.hasClient())
    {
      ChangeAirResistance(0);
    }
  
    if (BT.available() > 0)
    {
      String msg = "";
      while (BT.available() > 0)
      {
        char c = BT.read();
        msg += c;
      }
      ChangeAirResistance(msg.toInt());
    }
  }
}
