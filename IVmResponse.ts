export interface IVmResponse {
  answer: string; // plain text answer
  value: string; // in bespoke syntax
  // 1:option1,0:option2,0:option3 (<selected>:<option text>) for 'choice' type
}