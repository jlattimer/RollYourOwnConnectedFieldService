#include "SI7020-A20_CE_JL.h"

double SI7020_A20_CE_JL::temperatureC(){
    int raw = getRawTemperatureReading();
    double temperature = ((175.72*raw)/65536)-46.85;
    return temperature;
}

double SI7020_A20_CE_JL::temperatureF(){
    double degreesC = temperatureC();
    double temperatureF = (degreesC*1.8)+32;
    return temperatureF;
}

float SI7020_A20_CE_JL::humidity() {
    float raw = getRawHumidityReading();
    float humidity = ((125 * raw) / 65536.0) - 6;
    return humidity;
}

//Read temperature from SI7020
int SI7020_A20_CE_JL::getRawTemperatureReading(){
    Wire.begin();
    Wire.beginTransmission(address);
    Wire.write(temperatureRegister);
    byte status = Wire.endTransmission();
    if(status != 0){
        Serial.println("Read temperature failed");
        return 0;
    } else {
        //It worked
        Wire.requestFrom(address, 2);
        byte msb = Wire.read();
        byte lsb = Wire.read();
        int reading = (msb*256)+lsb;
        return reading;
    }
}

//Read humidity from SI7020
int SI7020_A20_CE_JL::getRawHumidityReading() {
    Wire.begin();
    Wire.beginTransmission(address);
    Wire.write(humidityRegister);
    byte status = Wire.endTransmission();

    if(status != 0){
        Serial.println("Read humidity failed");
        return 0;
    } else {
        //It worked
        Wire.requestFrom(address, 2);
        byte msb = Wire.read();
        byte lsb = Wire.read();
        float humidity  = ((msb * 256.0) + lsb);
        return humidity;
    }
}

uint8_t SI7020_A20_CE_JL::getHeater()
{
	return readRegister(readUserRegister1);
}

void SI7020_A20_CE_JL::turnHeaterOn() {
    uint8_t hStatus = getHeater();
    if (hStatus != heaterStatusOn) {
        //Turn on heater
        setHeater(1);
    }
}

void SI7020_A20_CE_JL::turnHeaterOff() {
    uint8_t hStatus = getHeater();
    if (hStatus != heaterStatusOff) {
        //Turn off heater
        setHeater(0);
    }
}

void SI7020_A20_CE_JL::setHeater(uint8_t hBit)
{
    uint8_t onOff = (hBit == 1) ? 0x3E : 0x3A;
    writeRegister(writeUserRegister1, onOff); 				// Write user register
}

void SI7020_A20_CE_JL::setHeaterPower(uint8_t hPow)
{
	uint8_t hReg = readRegister(0x11);						      // Read heater register
		if(hPow == 1)	hReg &= ~(0x0F);					          // Set current 3.09mA
		if(hPow == 2)	hReg = (hReg & ~(0x0F)) | (0x01);	  // Set current 9.18mA
		if(hPow == 3)	hReg = (hReg & ~(0x0F)) | (0x02);	  // Set current 15.24mA
		if(hPow == 4)	hReg = (hReg & ~(0x0F)) | (0x04);	  // Set current 27.39mA
		if(hPow == 5)	hReg = (hReg & ~(0x0F)) | (0x08);	  // Set current 51.69mA
		if(hPow == 6)	hReg |= (0x0F);						          // Set current 94.20mA
    writeRegister(0x51, hReg);								        // Write heater register
}

uint8_t SI7020_A20_CE_JL::getHeaterPower()
{
    uint8_t hReg = readRegister(0x11);			          // Read heater register
		if(hReg == 0x00)		return  3;					          // Return current 3mA
		if(hReg == 0x01)		return  9;					          // Return current 9mA
		if(hReg == 0x02)		return 15;					          // Return current 15mA
		if(hReg == 0x04)		return 27;					          // Return current 27mA
		if(hReg == 0x08)		return 51;					          // Return current 51mA
		if(hReg == 0x0F)		return 94;					          // Return current 94mA
    return 0;												                  // Return 0 for error
}

int SI7020_A20_CE_JL::resetSensor() {
    Wire.begin();
    Wire.beginTransmission(address);
	  Wire.write(resetRegister); 								        // Write reset command
    Wire.endTransmission();
    return 0;
}

uint8_t SI7020_A20_CE_JL::readRegister(uint8_t reg)
{
    Wire.begin();
    Wire.beginTransmission(address);
    Wire.write(reg);
    Wire.endTransmission(false);
    Wire.requestFrom(address, 1);
    return Wire.read();
}

void SI7020_A20_CE_JL::writeRegister(uint8_t reg, uint8_t value)
{
    Wire.begin();
    Wire.beginTransmission(address);
    Wire.write(reg);
    Wire.write(value);
    Wire.endTransmission();
}
